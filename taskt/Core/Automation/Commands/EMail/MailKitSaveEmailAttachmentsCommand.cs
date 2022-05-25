﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Serialization;
using taskt.UI.CustomControls;
using taskt.UI.Forms;
using taskt.Core.Automation.Attributes.PropertyAttributes;

namespace taskt.Core.Automation.Commands
{
    [Serializable]
    [Attributes.ClassAttributes.Group("EMail Commands")]
    [Attributes.ClassAttributes.SubGruop("")]
    [Attributes.ClassAttributes.Description("This command allows you to get Text from EMail.")]
    [Attributes.ClassAttributes.UsesDescription("Use this command when you want to get Text from EMail.")]
    [Attributes.ClassAttributes.ImplementationDescription("")]
    public class MailKitSaveEmailAttachmentsCommand : ScriptCommand
    {
        [XmlAttribute]
        [PropertyDescription("Please specify EMail Variable Name")]
        [PropertyUIHelper(PropertyUIHelper.UIAdditionalHelperType.ShowVariableHelper)]
        [InputSpecification("")]
        [SampleUsage("**{{{vEMail}}}**")]
        [Remarks("")]
        [PropertyShowSampleUsageInDescription(true)]
        [PropertyValidationRule("EMail", PropertyValidationRule.ValidationRuleFlags.Empty)]
        [PropertyRecommendedUIControl(PropertyRecommendedUIControl.RecommendeUIControlType.ComboBox)]
        public string v_MailName { get; set; }

        [XmlAttribute]
        [PropertyDescription("Please specify Path to the File")]
        [PropertyUIHelper(PropertyUIHelper.UIAdditionalHelperType.ShowVariableHelper)]
        [PropertyUIHelper(PropertyUIHelper.UIAdditionalHelperType.ShowFileSelectionHelper)]
        [InputSpecification("")]
        [SampleUsage("**vText** or **{{{vText}}}**")]
        [Remarks("")]
        [PropertyShowSampleUsageInDescription(true)]
        [PropertyRecommendedUIControl(PropertyRecommendedUIControl.RecommendeUIControlType.ComboBox)]
        [PropertyIsVariablesList(true)]
        [PropertyValidationRule("Text Variable", PropertyValidationRule.ValidationRuleFlags.Empty)]
        public string v_SaveFolder { get; set; }

        public MailKitSaveEmailAttachmentsCommand()
        {
            this.CommandName = "MailKitSaveEmailAttachmentsCommand";
            this.SelectionName = "Save Email Attachments";
            this.CommandEnabled = true;
            this.CustomRendering = true;
        }

        public override void RunCommand(object sender)
        {
            var engine = (Engine.AutomationEngineInstance)sender;

            var mail = v_MailName.GetMailKitMailVariable(engine);

            var path = v_SaveFolder.ConvertToUserVariable(engine);

            foreach(var attachment in mail.Attachments)
            {
                if (attachment is MimeKit.MimePart)
                {
                    var at = (MimeKit.MimePart)attachment;

                    using (var fs = System.IO.File.Create(System.IO.Path.Combine(path, at.FileName)))
                    {
                        at.Content.DecodeTo(fs);
                    }
                }
            }
        }
        public override List<Control> Render(frmCommandEditor editor)
        {
            base.Render(editor);

            var ctrls = CommandControls.MultiCreateInferenceDefaultControlGroupFor(this, editor);
            RenderedControls.AddRange(ctrls);

            return RenderedControls;
        }

        public override string GetDisplayValue()
        {
            return base.GetDisplayValue() + "";
        }
    }
}
