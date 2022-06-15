﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Serialization;
using taskt.UI.CustomControls;
using taskt.UI.Forms;

namespace taskt.Core.Automation.Commands
{
    [Serializable]
    [Attributes.ClassAttributes.Group("Programs/Process Commands")]
    [Attributes.ClassAttributes.Description("This command allows you to run a script or program and wait for it to exit before proceeding.")]
    [Attributes.ClassAttributes.UsesDescription("Use this command when you want to run a script (such as vbScript, javascript, or executable) but wait for it to close before taskt continues executing.")]
    [Attributes.ClassAttributes.ImplementationDescription("This command implements 'Process.Start' and waits for the script/program to exit before proceeding.")]
    public class RunScriptCommand : ScriptCommand
    {
        [XmlAttribute]
        [Attributes.PropertyAttributes.PropertyDescription("Enter the path to the script (ex. C:\\temp\\myscript.vbs, {{{vScriptPath}}})")]
        [Attributes.PropertyAttributes.PropertyUIHelper(Attributes.PropertyAttributes.PropertyUIHelper.UIAdditionalHelperType.ShowVariableHelper)]
        [Attributes.PropertyAttributes.PropertyUIHelper(Attributes.PropertyAttributes.PropertyUIHelper.UIAdditionalHelperType.ShowFileSelectionHelper)]
        [Attributes.PropertyAttributes.InputSpecification("Enter a fully qualified path to the script, including the script extension.")]
        [Attributes.PropertyAttributes.SampleUsage("**C:\\temp\\myscript.vbs** or **{{{vScriptPath}}}**")]
        [Attributes.PropertyAttributes.Remarks("This command differs from **Start Process** because this command blocks execution until the script has completed.  If you do not want to stop while the script executes, consider using **Start Process** instead.If file does not contain extensin, supplement extensions supported by cmd.\nIf file does not contain folder path, file will be opened in the same folder as script file.")]
        public string v_ScriptPath { get; set; }

        public RunScriptCommand()
        {
            this.CommandName = "RunScriptCommand";
            this.SelectionName = "Run Script";
            this.CommandEnabled = true;
            this.CustomRendering = true;
        }

        public override void RunCommand(object sender)
        {
            var engine = (Engine.AutomationEngineInstance)sender;

            //var scriptPath = v_ScriptPath.ConvertToUserVariable(sender);
            //scriptPath = Core.FilePathControls.formatFilePath(scriptPath, (Engine.AutomationEngineInstance)sender);
            //if (!System.IO.File.Exists(scriptPath) && !Core.FilePathControls.hasExtension(scriptPath))
            //{
            //    string[] exts = new string[] { ".bat", ".vbs", ".js", ".wsf" };
            //    foreach(string ext in exts)
            //    {
            //        if (System.IO.File.Exists(scriptPath + ext))
            //        {
            //            scriptPath += ext;
            //            break;
            //        }
            //    }
            //}
            string scriptPath = FilePathControls.formatFilePath_NoFileCounter(v_ScriptPath, engine, new List<string>() { "bat", "vbs", "js", "wsf" }, true);

            System.Diagnostics.Process scriptProc = new System.Diagnostics.Process();
            scriptProc.StartInfo.FileName = scriptPath;
            scriptProc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            scriptProc.Start();
            scriptProc.WaitForExit();

            scriptProc.Close();
        }

        public override List<Control> Render(frmCommandEditor editor)
        {
            base.Render(editor);

            RenderedControls.AddRange(CommandControls.CreateDefaultInputGroupFor("v_ScriptPath", this, editor));

            return RenderedControls;
        }

        public override string GetDisplayValue()
        {
            return base.GetDisplayValue() + " [Script Path: " + v_ScriptPath + "]";
        }

        public override bool IsValidate(frmCommandEditor editor)
        {
            base.IsValidate(editor);

            if (String.IsNullOrEmpty(this.v_ScriptPath))
            {
                this.validationResult += "Script is empty.\n";
                this.IsValid = false;
            }

            return this.IsValid;
        }
    }
}