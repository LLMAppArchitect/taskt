﻿//Copyright (c) 2019 Jason Bayldon
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Data;

namespace taskt.Core.Script
{
    #region Script and Variables

    public class Script
    {
        /// <summary>
        /// Contains user-defined variables
        /// </summary>
        public List<ScriptVariable> Variables { get; set; }
        /// <summary>
        /// Contains user-selected commands
        /// </summary>
        public List<ScriptAction> Commands;

        public ScriptInformation Info;

        public Script()
        {
            //initialize
            Variables = new List<ScriptVariable>();
            Commands = new List<ScriptAction>();
            Info = new ScriptInformation();
        }
        /// <summary>
        /// Returns a new 'Top-Level' command.  
        /// </summary>
        public ScriptAction AddNewParentCommand(Core.Automation.Commands.ScriptCommand scriptCommand)
        {
            ScriptAction newExecutionCommand = new ScriptAction() { ScriptCommand = scriptCommand };
            Commands.Add(newExecutionCommand);
            return newExecutionCommand;
        }

        /// <summary>
        /// Converts and serializes the user-defined commands into an XML file  
        /// </summary>
        public static Script SerializeScript(ListView.ListViewItemCollection scriptCommands, List<ScriptVariable> scriptVariables, ScriptInformation info, Core.EngineSettings engineSettings, string scriptFilePath = "")
        {
            var script = new Core.Script.Script();

            //save variables to file

            script.Variables = scriptVariables;
            script.Info = info;

            //save listview tags to command list

            int lineNumber = 1;

            List<Core.Script.ScriptAction> subCommands = new List<Core.Script.ScriptAction>();

            foreach (ListViewItem commandItem in scriptCommands)
            {
                var srcCommand = (Core.Automation.Commands.ScriptCommand)commandItem.Tag;
                srcCommand.IsDontSavedCommand = false;

                var command = srcCommand.Clone();
                command.LineNumber = lineNumber;

                if ((command is Core.Automation.Commands.BeginNumberOfTimesLoopCommand) || (command is Core.Automation.Commands.BeginContinousLoopCommand) || (command is Core.Automation.Commands.BeginListLoopCommand) || (command is Core.Automation.Commands.BeginIfCommand) || (command is Core.Automation.Commands.BeginMultiIfCommand) || (command is Core.Automation.Commands.BeginExcelDatasetLoopCommand) || (command is Core.Automation.Commands.TryCommand) || (command is Core.Automation.Commands.BeginLoopCommand) || (command is Core.Automation.Commands.BeginMultiLoopCommand))
                {
                    if (subCommands.Count == 0)  //if this is the first loop
                    {
                        //add command to root node
                        var newCommand = script.AddNewParentCommand(command);
                        //add to tracking for additional commands
                        subCommands.Add(newCommand);
                    }
                    else  //we are already looping so add to sub item
                    {
                        //get reference to previous node
                        var parentCommand = subCommands[subCommands.Count - 1];
                        //add as new node to previous node
                        var nextNodeParent = parentCommand.AddAdditionalAction(command);
                        //add to tracking for additional commands
                        subCommands.Add(nextNodeParent);
                    }
                }
                else if ((command is Core.Automation.Commands.EndLoopCommand) || (command is Core.Automation.Commands.EndIfCommand) || (command is Core.Automation.Commands.EndTryCommand))  //if current loop scenario is ending
                {
                    //get reference to previous node
                    var parentCommand = subCommands[subCommands.Count - 1];
                    //add to end command // DECIDE WHETHER TO ADD WITHIN LOOP NODE OR PREVIOUS NODE
                    parentCommand.AddAdditionalAction(command);
                    //remove last command since loop is ending
                    subCommands.RemoveAt(subCommands.Count - 1);
                }
                else if (subCommands.Count == 0) //add command as a root item
                {
                    script.AddNewParentCommand(command);
                }
                else //we are within a loop so add to the latest tracked loop item
                {
                    var parentCommand = subCommands[subCommands.Count - 1];
                    parentCommand.AddAdditionalAction(command);
                }

                //increment line number
                lineNumber++;
            }

            // Convert Intermediate
            if (engineSettings.ExportIntermediateXML)
            {
                foreach (var cmd in script.Commands)
                {
                    cmd.ConvertToIntermediate(engineSettings, scriptVariables);
                }
            }

            //output to xml file
            XmlSerializer serializer = new XmlSerializer(typeof(Script));
            var settings = new XmlWriterSettings
            {
                NewLineHandling = NewLineHandling.Entitize,
                Indent = true
            };

            //if output path was provided
            if (scriptFilePath != "")
            {
                //write to file
                using (System.IO.FileStream fs = System.IO.File.Create(scriptFilePath))
                {
                    using (XmlWriter writer = XmlWriter.Create(fs, settings))
                    {
                        serializer.Serialize(writer, script);
                    }
                }
            }

            return script;
        }
        /// <summary>
        /// Deserializes a valid XML file back into user-defined commands
        /// </summary>
        public static Script DeserializeFile(string scriptFilePath, Core.EngineSettings engineSettings)
        {
            XDocument xmlScript = XDocument.Load(scriptFilePath);

            // pre-convert
            convertOldScript(xmlScript);

            using (var reader = xmlScript.Root.CreateReader())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Script));
                Script des = (Script)serializer.Deserialize(reader);

                // release
                serializer = null;

                foreach (var cmd in des.Commands)
                {
                    cmd.ConvertToRaw(engineSettings);
                }

                xmlScript = null;

                return des;
            }

            //open file stream from file
            //using (System.IO.FileStream fs = new System.IO.FileStream(scriptFilePath, System.IO.FileMode.Open))
            //{
            //    //read and return data
            //    XmlReader reader = XmlReader.Create(fs);
            //    Script deserializedData = (Script)serializer.Deserialize(reader);

            //    // release
            //    serializer = null;

            //    foreach (var cmd in deserializedData.Commands)
            //    {
            //        cmd.ConvertToRaw(engineSettings);
            //    }

            //    return deserializedData;
            //}
        }
        /// <summary>
        /// Deserializes an XML string into user-defined commands (server sends a string to the client)
        /// </summary>
        public static Script DeserializeXML(string scriptXML)
        {
            try
            {
                using (System.IO.StringReader reader = new System.IO.StringReader(scriptXML))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Script));
                    Script deserializedData = (Script)serializer.Deserialize(reader);
                    return deserializedData;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string SerializeScript(List<Core.Automation.Commands.ScriptCommand> commands)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Script));

            var actions = new Script();
            foreach (taskt.Core.Automation.Commands.ScriptCommand cmd in commands)
            {
                actions.AddNewParentCommand(cmd);
            }
            using (var writer = new System.IO.StringWriter())
            {
                serializer.Serialize(writer, actions);
                actions = null;
                return writer.ToString();
            }
        }

        public static Script DeserializeScript(string scriptXML)
        {
            using (var reader = new System.IO.StringReader(scriptXML))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Script));
                var ret = (Script)serializer.Deserialize(reader);
                return ret;
            }
        }

        private static XDocument convertOldScript(XDocument doc)
        {
            //IEnumerable<XElement> cmds = from el in doc.Descendants("ScriptCommand")
            //                             where (string)el.Attribute("CommandName") == "ActivateWindowCommand"
            //                             select el;

            //foreach (var cmd in cmds)
            //{
            //    if (((string)cmd.Attribute("v_SearchMethod")).ToLower() == "start with")
            //    {
            //        cmd.Attribute("v_SearchMethod").SetValue("Starts with");
            //    }
            //}

            convertTo3_5_0_45(doc);
            convertTo3_5_0_46(doc);
            convertTo3_5_0_47(doc);
            convertTo3_5_0_50(doc);

            return doc;
        }

        private static XDocument convertTo3_5_0_45(XDocument doc)
        {
            // change "Start with" -> "Starts with", "End with" -> "Ends with"
            IEnumerable<XElement> cmdWindowNames = doc.Descendants("ScriptCommand").Where(el => (
                    (string)el.Attribute("CommandName") == "ActivateWindowCommand" ||
                    (string)el.Attribute("CommandName") == "CheckWindowNameExistsCommand" ||
                    (string)el.Attribute("CommandName") == "CloseWindowCommand" ||
                    (string)el.Attribute("CommandName") == "GetWindowNamesCommand" ||
                    (string)el.Attribute("CommandName") == "GetWindowPositionCommand" ||
                    (string)el.Attribute("CommandName") == "GetWindowStateCommand" ||
                    (string)el.Attribute("CommandName") == "MoveWindowCommand" ||
                    (string)el.Attribute("CommandName") == "ResizeWindowCommand" ||
                    (string)el.Attribute("CommandName") == "SetWindowStateCommand" ||
                    (string)el.Attribute("CommandName") == "WaitForWindowCommand" ||
                    (string)el.Attribute("CommandName") == "SendAdvancedKeyStrokesCommand" ||
                    (string)el.Attribute("CommandName") == "SendHotkeyCommand" ||
                    (string)el.Attribute("CommandName") == "SendKeysCommand" ||
                    (string)el.Attribute("CommandName") == "UIAutomationCommand"
                )
            );
            foreach (var cmd in cmdWindowNames)
            {
                if (cmd.Attribute("v_SearchMethod") != null)
                {
                    if (((string)cmd.Attribute("v_SearchMethod")).ToLower() == "start with")
                    {
                        cmd.SetAttributeValue("v_SearchMethod", "Starts with");
                    }
                    if (((string)cmd.Attribute("v_SearchMethod")).ToLower() == "end with")
                    {
                        cmd.SetAttributeValue("v_SearchMethod", "Ends with");
                    }
                }
            }

            // ExcelCreateDataset -> LoadDataTable
            IEnumerable<XElement> cmdExcels = doc.Descendants("ScriptCommand")
                .Where(el => ((string)el.Attribute("CommandName") == "ExcelCreateDatasetCommand"));
            XNamespace ns = "http://www.w3.org/2001/XMLSchema-instance";
            foreach (var cmd in cmdExcels)
            {
                cmd.SetAttributeValue("CommandName", "LoadDataTableCommand");
                cmd.SetAttributeValue(ns + "type", "LoadDataTableCommand");
            }

            return doc;
        }

        private static XDocument convertTo3_5_0_46(XDocument doc)
        {
            // AddToVariable -> AddListItem
            IEnumerable<XElement> cmdAddList = doc.Descendants("ScriptCommand")
                .Where(el => ((string)el.Attribute("CommandName") == "AddToVariableCommand"));
            XNamespace ns = "http://www.w3.org/2001/XMLSchema-instance";
            foreach (var cmd in cmdAddList)
            {
                cmd.SetAttributeValue("CommandName", "AddListItemCommand");
                cmd.SetAttributeValue(ns + "type", "AddListItemCommand");
            }

            // SetVariableIndex -> SetListIndex
            IEnumerable<XElement> cmdListIndex = doc.Descendants("ScriptCommand")
                .Where(el => ((string)el.Attribute("CommandName") == "SetVariableIndexCommand"));
            foreach (var cmd in cmdListIndex)
            {
                cmd.SetAttributeValue("CommandName", "SetListIndexCommand");
                cmd.SetAttributeValue(ns + "type", "SetListIndexCommand");
            }

            return doc;
        }
        private static XDocument convertTo3_5_0_47(XDocument doc)
        {
            // AddListItem.v_userVariableName, SetListIndex.v_userVariableName -> *.v_ListName
            IEnumerable<XElement> cmdIndex = doc.Descendants("ScriptCommand")
                .Where(el =>
                    ((string)el.Attribute("CommandName") == "AddListItemCommand") ||
                    ((string)el.Attribute("CommandName") == "SetListIndexCommand")
            );
            //XNamespace ns = "http://www.w3.org/2001/XMLSchema-instance";
            foreach (var cmd in cmdIndex)
            {
                var listNameAttr = cmd.Attribute("v_userVariableName");
                if (listNameAttr != null)
                {
                    cmd.SetAttributeValue("v_ListName", listNameAttr.Value);
                    listNameAttr.Remove();
                }
            }

            return doc;
        }
        private static XDocument convertTo3_5_0_50(XDocument doc)
        {
            // ParseJSONArray -> ConvertJSONToList
            IEnumerable<XElement> cmdAddList = doc.Descendants("ScriptCommand")
                .Where(el => ((string)el.Attribute("CommandName") == "ParseJSONArrayCommand"));
            XNamespace ns = "http://www.w3.org/2001/XMLSchema-instance";
            foreach (var cmd in cmdAddList)
            {
                cmd.SetAttributeValue("CommandName", "ConvertJSONToListCommand");
                cmd.SetAttributeValue(ns + "type", "ConvertJSONToListCommand");
            }

            return doc;
        }
    }

    public class ScriptAction
    {
        /// <summary>
        /// generic 'top-level' user-defined script command (ex. not nested)
        /// </summary>
        [XmlElement(Order = 1)]
        public Core.Automation.Commands.ScriptCommand ScriptCommand { get; set; }
        /// <summary>
        /// generic 'sub-level' commands (ex. nested commands within a loop)
        /// </summary>
        [XmlElement(Order = 2)]
        public List<ScriptAction> AdditionalScriptCommands { get; set; }
        /// <summary>
        /// adds a command as a nested command to a top-level command
        /// </summary>
        public ScriptAction AddAdditionalAction(Core.Automation.Commands.ScriptCommand scriptCommand)
        {
            if (AdditionalScriptCommands == null)
            {
                AdditionalScriptCommands = new List<ScriptAction>();
            }

            ScriptAction newExecutionCommand = new ScriptAction() { ScriptCommand = scriptCommand };
            AdditionalScriptCommands.Add(newExecutionCommand);
            return newExecutionCommand;
        }

        public void ConvertToIntermediate(Core.EngineSettings settings, List<ScriptVariable> variables)
        {
            ScriptCommand.convertToIntermediate(settings, variables);
            if (AdditionalScriptCommands != null && AdditionalScriptCommands.Count > 0)
            {
                foreach (var cmd in AdditionalScriptCommands)
                {
                    cmd.ConvertToIntermediate(settings, variables);
                }
            }
        }

        public void ConvertToRaw(Core.EngineSettings settings)
        {
            ScriptCommand.convertToRaw(settings);
            if (AdditionalScriptCommands != null && AdditionalScriptCommands.Count > 0)
            {
                foreach (var cmd in AdditionalScriptCommands)
                {
                    cmd.ConvertToRaw(settings);
                }
            }
        }
    }
    [Serializable]
    public class ScriptVariable
    {
        /// <summary>
        /// name that will be used to identify the variable
        /// </summary>
        public string VariableName { get; set; }
        /// <summary>
        /// index/position tracking for complex variables (list)
        /// </summary>
        [XmlIgnore]
        public int CurrentPosition = 0;
        /// <summary>
        /// value of the variable or current index
        /// </summary>
        public object VariableValue { get; set; }
        /// <summary>
        /// retrieve value of the variable
        /// </summary>
        public string GetDisplayValue(string requiredProperty = "")
        {

            if (VariableValue is string)
            {
                switch (requiredProperty)
                {
                    case "type":
                    case "Type":
                    case "TYPE":
                        return "BASIC";
                    default:
                        return (string)VariableValue;
                }

            }
            else if (VariableValue is DataTable)
            {
                DataTable dataTable = (DataTable)VariableValue;
                switch (requiredProperty)
                {
                    case "rows":
                    case "Rows":
                    case "ROWS":
                        return dataTable.Rows.ToString();
                    case "cols":
                    case "Cols":
                    case "COLS":
                    case "columns":
                    case "Columns":
                    case "COLUMNS":
                        return dataTable.Columns.ToString();
                    case "type":
                    case "Type":
                    case "TYPE":
                        return "DATATABLE";
                    default:
                        var dataRow = dataTable.Rows[CurrentPosition];
                        return Newtonsoft.Json.JsonConvert.SerializeObject(dataRow.ItemArray);
                }
            }
            else if (VariableValue is Dictionary<string, string>)
            {
                Dictionary<string, string> trgDic = (Dictionary<string, string>)VariableValue;
                switch (requiredProperty)
                {
                    case "count":
                    case "Count":
                    case "COUNT":
                        return trgDic.Values.Count.ToString();
                    case "type":
                    case "Type":
                    case "TYPE":
                        return "DICTIONARY";
                    default:
                        return (trgDic.Values.ToArray())[CurrentPosition];
                }
            }
            else
            {
                List<string> requiredValue = (List<string>)VariableValue;
                switch (requiredProperty)
                {
                    case "count":
                    case "Count":
                    case "COUNT":
                        return requiredValue.Count.ToString();
                    case "index":
                    case "Index":
                    case "INDEX":
                        return CurrentPosition.ToString();
                    case "tojson":
                    case "ToJson":
                    case "toJson":
                    case "TOJSON":
                        return Newtonsoft.Json.JsonConvert.SerializeObject(requiredValue);
                    case "topipe":
                    case "ToPipe":
                    case "toPipe":
                    case "TOPIPE":
                        return String.Join("|", requiredValue);
                    case "first":
                    case "First":
                    case "FIRST":
                        return requiredValue.FirstOrDefault();
                    case "last":
                    case "Last":
                    case "LAST":
                        return requiredValue.Last();
                    case "type":
                    case "Type":
                    case "TYPE":
                        return "LIST";
                    default:
                        return requiredValue[CurrentPosition];
                }
            }

        }
    }

    #endregion Script and Variables

    [Serializable]
    public class ScriptInformation
    {
        public string TasktVersion { get; set; }
        public string Author { get; set; }
        public DateTime LastRunTime { get; set; }
        public int RunTimes { get; set; }
        public string ScriptVersion { get; set; }
        public string Description { get; set; }
        public ScriptInformation()
        {
            this.TasktVersion = "";
            this.Author = "";
            this.LastRunTime = DateTime.Parse("1990-01-01T00:00:00");
            this.RunTimes = 0;
            this.ScriptVersion = "0.0.0";
            this.Description = "";
        }
    }
}