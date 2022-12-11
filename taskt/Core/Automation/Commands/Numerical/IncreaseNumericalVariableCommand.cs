﻿using System;
using System.Xml.Serialization;
using taskt.Core.Automation.Attributes.PropertyAttributes;

namespace taskt.Core.Automation.Commands
{
    [Serializable]
    [Attributes.ClassAttributes.Group("Numerical Commands")]
    [Attributes.ClassAttributes.Description("This command allows you to Increase Value in Numerical Variable.")]
    [Attributes.ClassAttributes.UsesDescription("Use this command when you want to Increase Value in Numerical Variable.")]
    [Attributes.ClassAttributes.ImplementationDescription("")]
    [Attributes.ClassAttributes.EnableAutomateRender(true)]
    [Attributes.ClassAttributes.EnableAutomateDisplayText(true)]
    public class IncreaseNumericalVariableCommand : ScriptCommand
    {
        [XmlAttribute]
        [PropertyDescription("Please specify Numerical Variable")]
        [PropertyUIHelper(PropertyUIHelper.UIAdditionalHelperType.ShowVariableHelper)]
        [InputSpecification("")]
        [Remarks("")]
        [PropertyDetailSampleUsage("**vNum**", "Specifiy Variable **vNum**")]
        [PropertyDetailSampleUsage("**{{{vNum}}}**", "Specifiy Variable **vNum**")]
        [PropertyDetailSampleUsage("**{{{{{{vNum}}}}}}**", "Specifiy Variable of Value of Variable **vNum**", false)]
        [PropertyShowSampleUsageInDescription(true)]
        [PropertyRecommendedUIControl(PropertyRecommendedUIControl.RecommendeUIControlType.ComboBox)]
        [PropertyIsVariablesList(true)]
        [PropertyValidationRule("Variable", PropertyValidationRule.ValidationRuleFlags.Empty)]
        [PropertyDisplayText(true, "Variable")]
        public string v_VariableName { get; set; }

        [XmlAttribute]
        [PropertyDescription("Please specify value to increase")]
        [PropertyUIHelper(PropertyUIHelper.UIAdditionalHelperType.ShowVariableHelper)]
        [InputSpecification("")]
        [SampleUsage("**100** or **{{{vValue}}}**")]
        [Remarks("")]
        [PropertyTextBoxSetting(1, false)]
        [PropertyIsOptional(true, "1")]
        [PropertyDisplayText(true, "Increase")]
        public string v_Value { get; set; }

        public IncreaseNumericalVariableCommand()
        {
            this.CommandName = "IncreaseNumericalVariableCommand";
            this.SelectionName = "Increase Numerical Variable";
            this.CommandEnabled = true;
            this.CustomRendering = true;
        }

        public override void RunCommand(object sender)
        {
            var engine = (Engine.AutomationEngineInstance)sender;

            string variableName;
            if (engine.engineSettings.isWrappedVariableMarker(v_VariableName))
            {
                variableName = v_VariableName;
            }
            else
            {
                variableName = engine.engineSettings.wrapVariableMarker(v_VariableName);
            }

            var variableValue = new PropertyConvertTag(variableName, "Variable Name").ConvertToUserVariableAsDecimal(engine);

            //var add = new PropertyConvertTag(v_Value, "v_Value", "Value").ConvertToUserVariableAsDecimal(this, engine);
            var add = this.ConvertToUserVariableAsDecimal(nameof(v_Value), "Value To Increase", engine);

            (variableValue + add).ToString().StoreInUserVariable(engine, variableName);
        }
    }
}