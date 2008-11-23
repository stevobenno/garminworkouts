using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using ZoneFiveSoftware.Common.Data.Fitness;
using GarminFitnessPlugin.Controller;

namespace GarminFitnessPlugin.Data
{
    class BasePowerTarget : ITarget
    {
        public abstract class IConcretePowerTarget : IPluginSerializable, IXMLSerializable, IDirty
        {
            public IConcretePowerTarget(PowerTargetType type, BasePowerTarget baseTarget)
            {
                Trace.Assert(type != PowerTargetType.HeartRateTargetTypeCount);

                m_Type = type;
                m_BaseTarget = baseTarget;
            }

            public override void Serialize(Stream stream)
            {
                stream.Write(BitConverter.GetBytes((Int32)Type), 0, sizeof(Int32));
            }

            public override void Deserialize_V0(Stream stream, DataVersion version)
            {
                // This is the code that was in ITarget in data V0.  Since we changed our
                //  inheritance structure between V0 and V1, we must also change where the
                //  loading is done. It happens ITarget didn't deserialize anything in V0,
                //  so this is empty
            }

            public virtual void Serialize(XmlNode parentNode, XmlDocument document)
            {
            }

            public virtual bool Deserialize(XmlNode parentNode)
            {
                return true;
            }

            protected void TriggerTargetChangedEvent(IConcretePowerTarget target, PropertyChangedEventArgs args)
            {
                if (target == BaseTarget.ConcreteTarget)
                {
                    BaseTarget.TriggerTargetChangedEvent(args);
                }
            }

            public PowerTargetType Type
            {
                get { return m_Type; }
            }

            public BasePowerTarget BaseTarget
            {
                get { return m_BaseTarget; }
            }

            public abstract bool IsDirty
            {
                get;
                set;
            }

            public enum PowerTargetType
            {
                [StepDescriptionStringProviderAttribute("PowerZoneTargetDescriptionText")]
                ZoneGTC = 0,
                [StepDescriptionStringProviderAttribute("PowerZoneTargetDescriptionText")]
                ZoneST,
                [StepDescriptionStringProviderAttribute("PowerRangeTargetDescriptionText")]
                Range,
                HeartRateTargetTypeCount
            };

            PowerTargetType m_Type;
            BasePowerTarget m_BaseTarget;
        }

        public BasePowerTarget(IStep parent)
            : base(ITarget.TargetType.Power, parent)
        {
            if (Options.UseSportTracksPowerZones)
            {
                ConcreteTarget = new PowerZoneSTTarget(this);
            }
            else
            {
                ConcreteTarget = new PowerZoneGTCTarget(this);
            }
        }

        public BasePowerTarget(Stream stream, DataVersion version, IStep parent)
            : this(parent)
        {
            Deserialize(stream, version);
        }

        public override void Serialize(Stream stream)
        {
            base.Serialize(stream);

            m_ConcreteTarget.Serialize(stream);
        }

        public override void Deserialize_V0(Stream stream, DataVersion version)
        {
            // In V0, we only have GTC zone type
            m_ConcreteTarget = new PowerZoneGTCTarget(stream, version, this);
        }

        public void Deserialize_V1(Stream stream, DataVersion version)
        {
            Byte[] intBuffer = new Byte[sizeof(UInt32)];
             BasePowerTarget.IConcretePowerTarget.PowerTargetType type;

            stream.Read(intBuffer, 0, sizeof(UInt32));
            type = ( BasePowerTarget.IConcretePowerTarget.PowerTargetType)BitConverter.ToUInt32(intBuffer, 0);

            switch(type)
            {
                case  BasePowerTarget.IConcretePowerTarget.PowerTargetType.ZoneGTC:
                    {
                        m_ConcreteTarget = new PowerZoneGTCTarget(stream, version, this);
                        break;
                    }
                case  BasePowerTarget.IConcretePowerTarget.PowerTargetType.ZoneST:
                    {
                        m_ConcreteTarget = new PowerZoneSTTarget(stream, version, this);
                        break;
                    }
                case  BasePowerTarget.IConcretePowerTarget.PowerTargetType.Range:
                    {
                        m_ConcreteTarget = new PowerRangeTarget(stream, version, this);
                        break;
                    }
                default:
                    {
                        Trace.Assert(false);
                        break;
                    }
            }
        }

        public override void Serialize(XmlNode parentNode, XmlDocument document)
        {
            base.Serialize(parentNode, document);

            XmlNode childNode;

            childNode = document.CreateElement("PowerZone");
            parentNode.AppendChild(childNode);

            ConcreteTarget.Serialize(childNode, document);
        }

        public override bool Deserialize(XmlNode parentNode)
        {
            if(base.Deserialize(parentNode))
            {
                if (parentNode.ChildNodes.Count == 1 && parentNode.FirstChild.Name == "PowerZone")
                {
                    XmlNode child = parentNode.FirstChild;

                    if (child.Attributes.Count == 1 && child.Attributes[0].Name == "xsi:type" &&
                        child.Attributes[0].Value == "PredefinedPowerZone_t")
                    {
                        // We have a GTC HR zone
                        ConcreteTarget = new PowerZoneGTCTarget(this);
                        return ConcreteTarget.Deserialize(child);
                    }
                    else if(child.Attributes.Count == 1 && child.Attributes[0].Name == "xsi:type" &&
                        child.Attributes[0].Value == "CustomPowerZone_t")
                    {
                        // We have either a range or a ST power zone but we can't tell before the
                        //  extension section so create a range and if it ends up being a ST
                        //  zone, replace it
                        ConcreteTarget = new PowerRangeTarget(this);

                        return ConcreteTarget.Deserialize(child); ;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        public override void HandleTargetOverride(XmlNode extensionNode)
        {
            // We got here so our target must be a range
            Trace.Assert(ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.Range);

            IZoneCategory referenceZones = Options.PowerZoneCategory;
            string zoneReferenceId = null;
            int zoneIndex = -1;

            for (int j = 0; j < extensionNode.ChildNodes.Count; ++j)
            {
                XmlNode childNode = extensionNode.ChildNodes[j];

                if (childNode.Name == "Id" && childNode.ChildNodes.Count == 1 &&
                    childNode.FirstChild.GetType() == typeof(XmlText))
                {
                    zoneReferenceId = childNode.FirstChild.Value;
                }
                else if (childNode.Name == "Index" && childNode.ChildNodes.Count == 1 &&
                    childNode.FirstChild.GetType() == typeof(XmlText) &&
                    Utils.IsTextIntegerInRange(childNode.FirstChild.Value, 0, (UInt16)(referenceZones.Zones.Count - 1)))
                {
                    zoneIndex = int.Parse(childNode.FirstChild.Value);
                }
            }

            if (zoneReferenceId == referenceZones.ReferenceId && zoneIndex != -1)
            {
                ConcreteTarget = new PowerZoneSTTarget(referenceZones.Zones[zoneIndex], this);
            }
        }

        public  BasePowerTarget.IConcretePowerTarget ConcreteTarget
        {
            get { return m_ConcreteTarget; }
            set
            {
                if (m_ConcreteTarget != value)
                {
                    m_ConcreteTarget = value;

                    TriggerTargetChangedEvent(new PropertyChangedEventArgs("ConcreteTarget"));
                }
            }
        }

        public override bool IsDirty
        {
            get { return ConcreteTarget.IsDirty; }
            set { Trace.Assert(false); }
        }

        private  BasePowerTarget.IConcretePowerTarget m_ConcreteTarget = null;
    }
}
