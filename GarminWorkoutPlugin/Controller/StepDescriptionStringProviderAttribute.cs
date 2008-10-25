using System;

namespace GarminWorkoutPlugin.Controller
{
    class StepDescriptionStringProviderAttribute : Attribute
    {
        public StepDescriptionStringProviderAttribute(string name)
        {
            m_StringName = name;
		}

        protected string m_StringName;

        public string StringName
        {
            get { return m_StringName; }
            set { m_StringName = value; }
		}

    }
}
