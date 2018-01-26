using System;
using System.Collections.Generic;
using System.Text;

namespace GpVersionDifferences
{
    public class clsOID
    {
        public enum ModificationReason
        {
            Unknown = 0,
            Insert = 1,
            Modify = 2,
            Delete = 3
        }

        public enum ChangeType
        {
            None = 0,
            Geometry = 1,
            Attribute = 2,
            Both = 3
        }

        private Int32 m_lngObjectID;
        private ModificationReason m_Reason;
        private ChangeType m_Change;
        private string m_strVersion;

        public Int32 ObjectID
        {
            get { return m_lngObjectID; }
            set { m_lngObjectID = value; }
        }

        public ModificationReason Reason
        {
            get { return m_Reason; }
            set { m_Reason = value; }
        }

        public ChangeType ModificationType
        {
            get { return m_Change; }
            set { m_Change = value; }
        }


        public string Version
        {
            get { return m_strVersion; }
            set { m_strVersion = value; }
        }
    }
}
