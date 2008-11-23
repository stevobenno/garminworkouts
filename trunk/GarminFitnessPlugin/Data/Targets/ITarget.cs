using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using GarminFitnessPlugin.Controller;
using System.ComponentModel;

namespace GarminFitnessPlugin.Data
{
    abstract class ITarget : IPluginSerializable, IXMLSerializable, IDirty
    {
        protected ITarget(TargetType type, IStep parent)
        {
            Trace.Assert(type != TargetType.TargetTypeCount);
            m_Type = type;
            m_ParentStep = parent;
        }

        protected ITarget(Stream stream, DataVersion version, IStep parent)
        {
            Deserialize(stream, version);
        }

        public override void Serialize(Stream stream)
        {
            stream.Write(BitConverter.GetBytes((Int32)Type), 0, sizeof(Int32));
        }

        public override void Deserialize_V0(Stream stream, DataVersion version)
        {
        }

        public virtual void Serialize(XmlNode parentNode, XmlDocument document)
        {
            XmlAttribute attribute = document.CreateAttribute("xsi", "type", Constants.xsins);

            attribute.Value = Constants.TargetTypeTCXString[(int)Type];
            parentNode.Attributes.Append(attribute);
        }

        public virtual bool Deserialize(XmlNode parentNode)
        {
            return true;
        }

        public abstract void HandleTargetOverride(XmlNode extensionNode);

        public TargetType Type
        {
            get { return m_Type; }
        }

        protected void TriggerTargetChangedEvent(PropertyChangedEventArgs args)
        {
            if (TargetChanged != null)
            {
                TargetChanged(this, args);
            }
        }

        public IStep ParentStep
        {
            get { return m_ParentStep; }
        }

        public abstract bool IsDirty
        {
            get;
            set;
        }

        public enum TargetType
        {
            [ComboBoxStringProviderAttribute("NullTargetComboBoxText")]
            Null = 0,
            [ComboBoxStringProviderAttribute("SpeedTargetComboBoxText")]
            Speed,
            [ComboBoxStringProviderAttribute("CadenceTargetComboBoxText")]
            Cadence,
            [ComboBoxStringProviderAttribute("HeartRateTargetComboBoxText")]
            HeartRate,
            [ComboBoxStringProviderAttribute("PowerTargetComboBoxText")]
            Power,
            TargetTypeCount
        }

        public delegate void TargetChangedEventHandler(ITarget modifiedTarget, PropertyChangedEventArgs changedProperty);
        public event TargetChangedEventHandler TargetChanged;

        private TargetType m_Type;
        private IStep m_ParentStep;
    }
}
