using System;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Reflection;


namespace Lumitech.Helpers
{

    //Singleton: There can only be one
    public sealed class Settings
    {
        private static Settings _instance = null;
        private static readonly object singletonLock = new object();

        XmlDocument doc;
        string fileName;
        string rootName;

        public static Settings GetInstance(string inifilename, string rootName, bool reRead)
        {
            lock (singletonLock)
            {
                if (_instance == null)
                {
                    _instance = new Settings(inifilename, rootName);
                }
                else
                {
                    if (reRead)
                        _instance.ForceReRead();
                }
                return _instance;
            }
        }

        public static Settings GetInstance()
        {
            return GetInstance(false);
        }

        public static Settings GetInstance(bool ForceReread)
        {
            string fname = AppDomain.CurrentDomain.BaseDirectory + Environment.MachineName + "_settings.xml";
            return Settings.GetInstance(fname, "Settings", ForceReread);
        }

        private Settings(string fileName, string rootName)
        {
            this.fileName = fileName;
            this.rootName = rootName;

            doc = new XmlDocument();

            try
            {
                //if the specifc file does not exist, try to load the "General" File
                if (!File.Exists(this.fileName))
                    this.fileName = AppDomain.CurrentDomain.BaseDirectory + "Settings.xml";

                doc.Load(this.fileName);
            }
            catch (System.IO.FileNotFoundException)
            {
                CreateSettingsDocument();
                Flush();
            }
            catch
            {
                throw;
            }
        }

        private void ForceReRead()
        {
            doc.Load(fileName);
        }

        //Deletes all entries of a section
        public void ClearSection(string section)
        {
            XmlNode root = doc.DocumentElement;
            XmlNode s = root.SelectSingleNode('/' + rootName + '/' + section);

            if (s == null)
                return;  //not found

            s.RemoveAll();
        }

        //initializes a new settings file with the XML version
        //and the root node
        private void CreateSettingsDocument()
        {
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
            doc.AppendChild(doc.CreateElement(rootName));
        }

        public void Flush()
        {
            doc.Save(fileName);
        }

        //removes a section and all its entries
        public void RemoveSection(string section)
        {
            XmlNode root = doc.DocumentElement;
            XmlNode s = root.SelectSingleNode('/' + rootName + '/' + section);

            if (s != null)
                root.RemoveChild(s);
        }

        public void Save()
        {
            Flush();
        }


        #region Read methods

        public string ReadString(string section, string name, string defaultValue)
        {
            try
            {
                XmlNode root = doc.DocumentElement;
                XmlNode s = root.SelectSingleNode('/' + rootName + '/' + section);

                if (s == null)
                    return defaultValue;  //not found

                XmlNode n = s.SelectSingleNode(name);

                if (n == null)
                    return defaultValue;  //not found

                XmlAttributeCollection attrs = n.Attributes;

                foreach (XmlAttribute attr in attrs)
                {
                    if (attr.Name == "value")
                        return attr.Value;
                }
            }
            catch  { }

            return defaultValue;
        }

        //FW 21.4.2013 - Probiers mal mit Generic
        public T Read<T>(string section, string name, T defaultValue)
        {
            T ret = defaultValue;
            string s = ReadString(section, name, "");

            if (s == "") return defaultValue;

            try
            {
                if (typeof(T) == typeof(string))
                {
                    ret = (T)Convert.ChangeType(s, typeof(T));
                }
                else
                {
                    MethodInfo m = typeof(T).GetMethod("Parse", new Type[] { typeof(string) });
                    if (m != null) { ret = (T)m.Invoke(null, new object[] { s }); }
                }

                return ret;
            }
            catch (FormatException)
            {
                return defaultValue;
            }

        }

        public string ReadStringAttrib(string path, string atrribname, string defaultValue)
        {
            XmlNode root = doc.DocumentElement;
            XmlNode s = root.SelectSingleNode('/' + rootName + '/' + path);

            if (s == null)
                return defaultValue;  //not found

            XmlAttributeCollection attrs = s.Attributes;

            foreach (XmlAttribute attr in attrs)
            {
                if (attr.Name == atrribname)
                    return attr.Value;
            }

            return defaultValue;
        }

        //FW 21.4.2013 - Probiers mal mit Generic
        public T ReadAttrib<T>(string path, string attribname, T defaultValue)
        {
            T ret = defaultValue;
            string s = ReadStringAttrib(path, attribname, "");

            if (s == "") return defaultValue;

            try
            {
                if (typeof(T) == typeof(string))
                {
                    ret = (T)Convert.ChangeType(s, typeof(T));
                }
                else
                {
                    MethodInfo m = typeof(T).GetMethod("Parse", new Type[] { typeof(string) });
                    if (m != null) { ret = (T)m.Invoke(null, new object[] { s }); }
                }

                return ret;
            }
            catch (FormatException)
            {
                return defaultValue;
            }

        }

        #endregion


        #region Write methods

        public void WriteString(string section, string name, string value)
        {
            XmlNode root = doc.DocumentElement;
            XmlNode s = root.SelectSingleNode('/' + rootName + '/' + section);

            if (s == null)
                s = root.AppendChild(doc.CreateElement(section));

            XmlNode n = s.SelectSingleNode(name);

            if (n == null)
                n = s.AppendChild(doc.CreateElement(name));

            XmlAttribute attr = ((XmlElement)n).SetAttributeNode("value", "");
            attr.Value = value;
        }

        //FW 21.4.2013 - Probiers mal mit Generic
        public void Write<T>(string section, string name, T value)
        {
            WriteString(section, name, value.ToString());
        }

        #endregion
    }
}
