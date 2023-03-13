﻿using System;
using System.Xml.Serialization;
using taskt.Core.Automation.Attributes.PropertyAttributes;

namespace taskt.Core.Automation.Commands
{
    [Serializable]
    [Attributes.ClassAttributes.Group("Text Commands")]
    [Attributes.ClassAttributes.SubGruop("Check/Get")]
    [Attributes.ClassAttributes.CommandSettings("Get Word Length")]
    [Attributes.ClassAttributes.Description("This command allows you to retrieve the length of a Text or Variable.")]
    [Attributes.ClassAttributes.UsesDescription("Use this command when you want to find the length of a Text or Variable")]
    [Attributes.ClassAttributes.ImplementationDescription("")]
    [Attributes.ClassAttributes.EnableAutomateRender(true)]
    [Attributes.ClassAttributes.EnableAutomateDisplayText(true)]
    public class GetWordLengthCommand : ScriptCommand
    {
        [XmlAttribute]
        [PropertyDescription("Text")]
        [InputSpecification("Text", true)]
        [Remarks("")]
        [PropertyDetailSampleUsage("**Hello**", PropertyDetailSampleUsage.ValueType.Value, "Text")]
        [PropertyDetailSampleUsage("**{{{vText}}}**", PropertyDetailSampleUsage.ValueType.VariableValue, "Text")]
        [PropertyShowSampleUsageInDescription(true)]
        [PropertyParameterDirection(PropertyParameterDirection.ParameterDirection.Input)]
        [PropertyDisplayText(true, "Text")]
        public string v_InputValue { get; set; }

        [XmlAttribute]
        [PropertyVirtualProperty(nameof(GeneralPropertyControls), nameof(GeneralPropertyControls.v_Result))]
        public string v_applyToVariableName { get; set; }

        public GetWordLengthCommand()
        {
            //this.CommandName = "GetLengthCommand";
            //this.SelectionName = "Get Word Length";
            //this.CommandEnabled = true;
            //this.CustomRendering = true;
        }

        public override void RunCommand(object sender)
        {
            //get engine
            var engine = (Engine.AutomationEngineInstance)sender;

            //get input value
            var stringRequiringLength = v_InputValue.ConvertToUserVariable(engine);

            ////count number of words
            //var stringLength = stringRequiringLength.Length;

            //store word count into variable
            stringRequiringLength.Length.ToString().StoreInUserVariable(engine, v_applyToVariableName);
        }
    }
}