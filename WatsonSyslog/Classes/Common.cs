using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using Newtonsoft.Json; 

namespace WatsonSyslog
{
    /// <summary>
    /// Commonly used static methods.
    /// </summary>
    public static class Common
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructor

        #endregion

        #region Public-Internal-Classes

        #endregion

        #region Private-Internal-Classes

        #endregion

        #region Public-Methods
         
        #region Serialization

        public static string SerializeJsonBuiltIn(object obj)
        {
            if (obj == null) return null;

            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = Int32.MaxValue;
            return ser.Serialize(obj);
        }

        public static string SerializeJson(object obj)
        {
            if (obj == null) return null;
            string json = JsonConvert.SerializeObject(
                obj,
                Newtonsoft.Json.Formatting.Indented,
                new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });

            return json;
        }

        public static T DeserializeJsonBuiltIn<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));

            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                ser.MaxJsonLength = Int32.MaxValue;
                return ser.Deserialize<T>(json);
            }
            catch (Exception)
            {
                Console.WriteLine("");
                Console.WriteLine("Exception while deserializing:");
                Console.WriteLine(json);
                Console.WriteLine("");
                throw;
            }
        }

        public static T DeserializeJsonBuiltIn<T>(byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            return DeserializeJsonBuiltIn<T>(Encoding.UTF8.GetString(data));
        }

        public static T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception)
            {
                Console.WriteLine("");
                Console.WriteLine("Exception while deserializing:");
                Console.WriteLine(json);
                Console.WriteLine("");
                throw;
            }
        }

        public static T DeserializeJson<T>(byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            return DeserializeJson<T>(Encoding.UTF8.GetString(data));
        }

        public static T DeserializeXml<T>(string xml)
        {
            try
            {
                if (String.IsNullOrEmpty(xml)) throw new Exception();
                XmlSerializer xmls = new XmlSerializer(typeof(T));
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(SanitizeXml(xml))))
                {
                    return (T)xmls.Deserialize(ms);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T CopyObject<T>(object o)
        {
            if (o == null) return default(T);
            string json = SerializeJson(o);
            T ret = DeserializeJson<T>(json);
            return ret;
        }

        public static string XmlEscape(string val)
        {
            if (String.IsNullOrEmpty(val)) return null;
            XmlDocument doc = new XmlDocument();
            var node = doc.CreateElement("root");
            node.InnerText = val;
            return node.InnerXml;
        }

        public static string SanitizeXml(string xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            string ret = "";
            XmlDocument doc = new XmlDocument();
            using (StringReader sr = new StringReader(xml))
            {
                using (XmlTextReader xtr = new XmlTextReader(sr) { Namespaces = false })
                {
                    doc.LoadXml(xml);
                }
            }

            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter xtw = XmlWriter.Create(sw))
                {
                    doc.WriteTo(xtw);
                    xtw.Flush();

                    ret = sw.GetStringBuilder().ToString();
                }
            }

            if (String.IsNullOrEmpty(ret)) return null;

            // remove all namespaces
            XElement xe = XmlRemoveNamespace(XElement.Parse(xml));

            // remove null fields from string
            Regex rgx = new Regex("\\n*\\s*<([\\w_]+)></([\\w_]+)>\\n*");

            return rgx.Replace(xe.ToString(), "");
        }

        public static string QueryXml(string xml, string path)
        {
            try
            {
                if (String.IsNullOrEmpty(xml)) return null;
                if (String.IsNullOrEmpty(path)) return null;

                string sanitized = SanitizeXml(xml);
                StringReader sr = new StringReader(sanitized);
                XPathDocument xpd = new XPathDocument(sr);
                XPathNavigator xpn = xpd.CreateNavigator();
                XPathNodeIterator xni = xpn.Select(path);
                string response = null;

                while (xni.MoveNext())
                {
                    if (xni.Current.SelectSingleNode("*") != null)
                    {
                        response = QueryXmlProcessChildren(xni);
                    }
                    else
                    {
                        response = xni.Current.Value;
                    }
                }

                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static XElement XmlRemoveNamespace(XElement xml)
        {
            try
            {
                xml.RemoveAttributes();
                if (!xml.HasElements)
                {
                    XElement xe = new XElement(xml.Name.LocalName);
                    xe.Value = xml.Value;

                    foreach (XAttribute attribute in xml.Attributes())
                        xe.Add(attribute);

                    return xe;
                }
                return new XElement(xml.Name.LocalName, xml.Elements().Select(el => XmlRemoveNamespace(el)));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string QueryXmlProcessChildren(XPathNodeIterator xpni)
        {
            try
            {
                XPathNodeIterator child = xpni.Current.SelectChildren(XPathNodeType.All);

                while (child.MoveNext())
                {
                    if (child.Current.SelectSingleNode("*") != null) QueryXmlProcessChildren(child);
                }

                return child.Current.Value;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Conversion

        public static string StringListToCsv(List<string> vals)
        {
            try
            {
                if (vals == null) return null;
                if (vals.Count < 1) return null;
                string ret = "";
                int count = 0;

                foreach (string curr in vals)
                {
                    if (count == 0)
                    {
                        ret += curr;
                    }
                    else
                    {
                        ret += "," + curr;
                    }

                    count++;
                }

                return ret;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<int> CsvToIntList(string csv)
        {
            try
            {
                if (String.IsNullOrEmpty(csv)) return null;

                List<int> ret = new List<int>();

                string[] array = csv.Split(',');

                if (array != null)
                {
                    if (array.Length > 0)
                    {
                        foreach (string curr in array)
                        {
                            if (String.IsNullOrEmpty(curr)) continue;

                            int val = 0;
                            if (!Int32.TryParse(curr, out val))
                            {
                                return null;
                            }

                            ret.Add(val);
                        }

                        return ret;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<string> CsvToStringList(string csv)
        {
            try
            {
                if (String.IsNullOrEmpty(csv)) return null;

                List<string> ret = new List<string>();

                string[] array = csv.Split(',');

                if (array != null)
                {
                    if (array.Length > 0)
                    {
                        foreach (string curr in array)
                        {
                            if (String.IsNullOrEmpty(curr)) continue;
                            ret.Add(curr.Trim());
                        }

                        return ret;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] StreamToBytes(Stream input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!input.CanRead) throw new InvalidOperationException("Input stream is not readable");

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

        public static List<object> ObjectToList(object obj)
        {
            if (obj == null) return null;
            if (obj is IEnumerable)
            {
                List<object> list = new List<object>();
                var enumerator = ((IEnumerable)obj).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    list.Add(enumerator.Current);
                }
                return list;
            }
            return null;
        }

        public static byte[] ObjectToBytes(object obj)
        {
            try
            {
                if (obj == null) return null;
                if (obj is byte[]) return (byte[])obj;

                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<T> GenericToSpecificList<T>(List<object> source)
        {
            if (source == null) return null;

            List<T> retList = new List<T>();
            int count = 0;

            foreach (object curr in source)
            {
                retList.Add((T)curr);
                count++;
            }

            return retList;
        }

        public static List<object> DataTableToListObject(DataTable dt, string objType)
        {
            //
            // Must pass in the fully-qualified class name including namespace
            //
            // i.e. Namespace.ClassName
            //

            if (dt == null) return null;
            if (dt.Rows.Count < 1) return null;
            if (String.IsNullOrEmpty(objType)) return null;

            List<object> retList = new List<object>();
            int count = 0;

            foreach (DataRow currRow in dt.Rows)
            {
                object ret = Activator.CreateInstance(Type.GetType(objType));
                if (ret == null) return null;

                foreach (PropertyInfo prop in ret.GetType().GetProperties())
                {
                    #region Process-Each-Property

                    PropertyInfo tempProp = prop;

                    switch (prop.PropertyType.ToString().ToLower().Trim())
                    {
                        case "system.string":
                            string valStr = currRow[prop.Name].ToString().Trim();
                            tempProp.SetValue(ret, valStr, null);
                            break;

                        case "system.int32":
                        case "system.nullable`1[system.int32]":
                            int valInt32 = 0;
                            if (Int32.TryParse(currRow[prop.Name].ToString(), out valInt32)) tempProp.SetValue(ret, valInt32, null);
                            break;

                        case "system.int64":
                        case "system.nullable`1[system.int64]":
                            long valInt64 = 0;
                            if (Int64.TryParse(currRow[prop.Name].ToString(), out valInt64)) tempProp.SetValue(ret, valInt64, null);
                            break;

                        case "system.decimal":
                        case "system.nullable`1[system.decimal]":
                            decimal valDecimal = 0m;
                            if (Decimal.TryParse(currRow[prop.Name].ToString(), out valDecimal)) tempProp.SetValue(ret, valDecimal, null);
                            break;

                        case "system.datetime":
                        case "system.nullable`1[system.datetime]":
                            DateTime datetime = DateTime.Now;
                            if (DateTime.TryParse(currRow[prop.Name].ToString(), out datetime)) tempProp.SetValue(ret, datetime, null);
                            break;

                        default:
                            break;
                    }

                    #endregion
                }

                count++;
                retList.Add(ret);
            }

            return retList;
        }

        public static object DataTableToObject(DataTable dt, string objType)
        {
            if (dt == null) return null;
            if (dt.Rows.Count != 1) return null;
            if (String.IsNullOrEmpty(objType)) return null;

            object ret = new object();

            foreach (DataRow dr in dt.Rows)
            {
                ret = Activator.CreateInstance(Type.GetType(objType));
                if (ret == null)
                {
                    return null;
                }

                foreach (PropertyInfo prop in ret.GetType().GetProperties())
                {
                    PropertyInfo tempProp = prop;

                    switch (prop.PropertyType.ToString().ToLower().Trim())
                    {
                        case "system.string":
                            string valStr = dr[prop.Name].ToString().Trim();
                            tempProp.SetValue(ret, valStr, null);
                            break;

                        case "system.int32":
                        case "system.nullable`1[system.int32]":
                            int valInt32 = 0;
                            if (Int32.TryParse(dr[prop.Name].ToString(), out valInt32)) tempProp.SetValue(ret, valInt32, null);
                            break;

                        case "system.int64":
                        case "system.nullable`1[system.int64]":
                            long valInt64 = 0;
                            if (Int64.TryParse(dr[prop.Name].ToString(), out valInt64)) tempProp.SetValue(ret, valInt64, null);
                            break;

                        case "system.decimal":
                        case "system.nullable`1[system.decimal]":
                            decimal valDecimal = 0m;
                            if (Decimal.TryParse(dr[prop.Name].ToString(), out valDecimal)) tempProp.SetValue(ret, valDecimal, null);
                            break;

                        case "system.datetime":
                        case "system.nullable`1[system.datetime]":
                            DateTime datetime = DateTime.Now;
                            if (DateTime.TryParse(dr[prop.Name].ToString(), out datetime)) tempProp.SetValue(ret, datetime, null);
                            break;

                        default:
                            break;
                    }
                }

                break;
            }

            return ret;
        }

        public static T DataTableToObject<T>(this DataTable table) where T : new()
        {
            IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            IList<T> result = new List<T>();

            foreach (var row in table.Rows)
            {
                var item = CreateItemFromRow<T>((DataRow)row, properties);
                return item;
            }

            return default(T);
        }

        public static IList<T> DataTableToList<T>(this DataTable table) where T : new()
        {
            IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            IList<T> result = new List<T>();

            foreach (var row in table.Rows)
            {
                var item = CreateItemFromRow<T>((DataRow)row, properties);
                result.Add(item);
            }

            return result;
        }

        public static IList<T> DataTableToList<T>(this DataTable table, Dictionary<string, string> mappings) where T : new()
        {
            IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            IList<T> result = new List<T>();

            foreach (var row in table.Rows)
            {
                var item = CreateItemFromRow<T>((DataRow)row, properties, mappings);
                result.Add(item);
            }

            return result;
        }

        private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties) where T : new()
        {
            T item = new T();
            foreach (var property in properties)
            {
                if (row[property.Name] is System.DBNull) continue;
                property.SetValue(item, row[property.Name], null);
            }
            return item;
        }

        private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties, Dictionary<string, string> mappings) where T : new()
        {
            T item = new T();
            foreach (var property in properties)
            {
                if (mappings.ContainsKey(property.Name))
                    property.SetValue(item, row[mappings[property.Name]], null);
            }
            return item;
        }

        public static List<dynamic> DataTableToListDynamic(DataTable dt)
        {
            List<dynamic> ret = new List<dynamic>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                dynamic dyn = new ExpandoObject();
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)dyn;
                    dic[col.ColumnName] = curr[col];
                }
                ret.Add(dyn);
            }

            return ret;
        }

        public static dynamic DataTableToDynamic(DataTable dt)
        {
            dynamic ret = new ExpandoObject();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)ret;
                    dic[col.ColumnName] = curr[col];
                }

                return ret;
            }

            return ret;
        }

        public static List<Dictionary<string, object>> DataTableToListDictionary(DataTable dt)
        {
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                Dictionary<string, object> currDict = new Dictionary<string, object>();

                foreach (DataColumn col in dt.Columns)
                {
                    currDict.Add(col.ColumnName, curr[col]);
                }

                ret.Add(currDict);
            }

            return ret;
        }

        public static Dictionary<string, object> DataTableToDictionary(DataTable dt)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    ret.Add(col.ColumnName, curr[col]);
                }

                return ret;
            }

            return ret;
        }

        public static bool DataTableIsNullOrEmpty(DataTable table)
        {
            if (table == null) return true;
            if (table.Rows.Count < 1) return true;
            return false;
        }

        public static string DecimalToString(object obj)
        {
            if (obj == null) return null;
            string ret = string.Format("{0:N2}", obj);
            ret = ret.Replace(",", "");
            return ret;
        }

        public static decimal DecimalRoundUp(decimal d)
        {
            decimal p = d * 100;
            decimal q = Math.Ceiling(p);
            return (q / 100);
        }

        public static string DbTimestamp(string dbType, DateTime ts)
        {
            if (String.IsNullOrEmpty(dbType)) return ts.ToString("MM/dd/yyyy HH:mm:ss.ffffff tt");

            switch (dbType)
            {
                case "mysql":
                    return ts.ToString("yyyy-MM-dd HH:mm:ss.ffffff");

                case "mssql":
                default:
                    return ts.ToString("MM/dd/yyyy hh:mm:ss.fffffff tt");
            }
        }

        #endregion

        #region Misc

        public static double TotalMsFrom(DateTime start)
        {
            DateTime end = DateTime.Now.ToUniversalTime();
            TimeSpan total = (end - start);
            return total.TotalMilliseconds;
        }

        public static bool UrlEqual(string url1, string url2, bool includeIntegers)
        {
            /* 
             * 
             * Takes two URLs as input and tokenizes.  Token demarcation characters
             * are question mark ?, slash /, ampersand &, and colon :.
             * 
             * Integers are allowed as tokens if include_integers is set to true.
             * 
             * Tokens are whitespace-trimmed and converted to lowercase.
             * 
             * At the end, the token list for each URL is compared.
             * 
             * Returns TRUE if contents same
             * Returns FALSE otherwise
             * 
             */

            if (String.IsNullOrEmpty(url1)) throw new ArgumentNullException(nameof(url1));
            if (String.IsNullOrEmpty(url2)) throw new ArgumentNullException(nameof(url2));

            string currString = "";
            int currStringInt = 0;
            List<string> url1Tokens = new List<string>();
            List<string> url2Tokens = new List<string>();
            string[] url1TokensArray;
            string[] url2TokensArray;

            #region Build-Token-Lists

            #region url1

            #region Iterate

            for (int i = 0; i < url1.Length; i++)
            {
                #region Slash-or-Colon

                if ((url1[i] == '/')        // slash
                    || (url1[i] == ':'))    // colon
                {
                    if (String.IsNullOrEmpty(currString))
                    {
                        #region Nothing-to-Add

                        continue;

                        #endregion
                    }
                    else
                    {
                        #region Something-to-Add

                        currStringInt = 0;
                        if (int.TryParse(currString, out currStringInt))
                        {
                            #region Integer

                            if (includeIntegers)
                            {
                                url1Tokens.Add(String.Copy(currString.ToLower().Trim()));
                            }

                            currString = "";
                            continue;

                            #endregion
                        }
                        else
                        {
                            #region Not-an-Integer

                            url1Tokens.Add(String.Copy(currString.ToLower().Trim()));
                            currString = "";
                            continue;

                            #endregion
                        }

                        #endregion
                    }
                }

                #endregion

                #region Question-or-Ampersand

                if ((url1[i] == '?')        // question
                    || (url1[i] == '&'))    // ampersand
                {
                    if (String.IsNullOrEmpty(currString))
                    {
                        #region Nothing-to-Add

                        break;

                        #endregion
                    }
                    else
                    {
                        #region Something-to-Add

                        currStringInt = 0;
                        if (int.TryParse(currString, out currStringInt))
                        {
                            #region Integer

                            if (includeIntegers)
                            {
                                url1Tokens.Add(String.Copy(currString.ToLower().Trim()));
                            }

                            currString = "";
                            break;

                            #endregion
                        }
                        else
                        {
                            #region Not-an-Integer

                            url1Tokens.Add(String.Copy(currString.ToLower().Trim()));
                            currString = "";
                            break;

                            #endregion
                        }

                        #endregion
                    }
                }

                #endregion

                #region Add-Characters

                currString += url1[i];
                continue;

                #endregion
            }

            #endregion

            #region Remainder

            if (!String.IsNullOrEmpty(currString))
            {
                #region Something-to-Add

                currStringInt = 0;
                if (int.TryParse(currString, out currStringInt))
                {
                    #region Integer

                    if (includeIntegers)
                    {
                        url1Tokens.Add(String.Copy(currString.ToLower().Trim()));
                    }

                    currString = "";

                    #endregion
                }
                else
                {
                    #region Not-an-Integer

                    url1Tokens.Add(String.Copy(currString.ToLower().Trim()));
                    currString = "";

                    #endregion
                }

                #endregion
            }

            #endregion

            #region Convert-and-Enumerate

            url1TokensArray = url1Tokens.ToArray();

            #endregion

            #endregion

            #region url2

            #region Iterate

            for (int i = 0; i < url2.Length; i++)
            {
                #region Slash-or-Colon

                if ((url2[i] == '/')        // slash
                    || (url2[i] == ':'))    // colon
                {
                    if (String.IsNullOrEmpty(currString))
                    {
                        #region Nothing-to-Add

                        continue;

                        #endregion
                    }
                    else
                    {
                        #region Something-to-Add

                        currStringInt = 0;
                        if (int.TryParse(currString, out currStringInt))
                        {
                            #region Integer

                            if (includeIntegers)
                            {
                                url2Tokens.Add(String.Copy(currString.ToLower().Trim()));
                            }

                            currString = "";
                            continue;

                            #endregion
                        }
                        else
                        {
                            #region Not-an-Integer

                            url2Tokens.Add(String.Copy(currString.ToLower().Trim()));
                            currString = "";
                            continue;

                            #endregion
                        }

                        #endregion
                    }
                }

                #endregion

                #region Question-or-Ampersand

                if ((url2[i] == '?')        // question
                    || (url2[i] == '&'))    // ampersand
                {
                    if (String.IsNullOrEmpty(currString))
                    {
                        #region Nothing-to-Add

                        break;

                        #endregion
                    }
                    else
                    {
                        #region Something-to-Add

                        currStringInt = 0;
                        if (int.TryParse(currString, out currStringInt))
                        {
                            #region Integer

                            if (includeIntegers)
                            {
                                url2Tokens.Add(String.Copy(currString.ToLower().Trim()));
                            }

                            currString = "";
                            break;

                            #endregion
                        }
                        else
                        {
                            #region Not-an-Integer

                            url2Tokens.Add(String.Copy(currString.ToLower().Trim()));
                            currString = "";
                            break;

                            #endregion
                        }

                        #endregion
                    }
                }

                #endregion

                #region Add-Characters

                currString += url2[i];
                continue;

                #endregion
            }

            #endregion

            #region Remainder

            if (!String.IsNullOrEmpty(currString))
            {
                #region Something-to-Add

                currStringInt = 0;
                if (int.TryParse(currString, out currStringInt))
                {
                    #region Integer

                    if (includeIntegers)
                    {
                        url2Tokens.Add(String.Copy(currString.ToLower().Trim()));
                    }

                    currString = "";

                    #endregion
                }
                else
                {
                    #region Not-an-Integer

                    url2Tokens.Add(String.Copy(currString.ToLower().Trim()));
                    currString = "";

                    #endregion
                }

                #endregion
            }

            #endregion

            #region Convert-and-Enumerate

            url2TokensArray = url2Tokens.ToArray();

            #endregion

            #endregion

            #endregion

            #region Compare-and-Return

            if (url1Tokens == null) return false;
            if (url2Tokens == null) return false;
            if (url1Tokens.Count != url2Tokens.Count) return false;

            for (int i = 0; i < url1Tokens.Count; i++)
            {
                if (String.Compare(url1TokensArray[i], url2TokensArray[i]) != 0)
                {
                    return false;
                }
            }

            return true;

            #endregion
        }

        public static void DescribeObject(object o)
        {
            Console.WriteLine("");
            Console.WriteLine("");
            PropertyInfo[] p = o.GetType().GetProperties();

            Console.WriteLine("Object contains the following properties:");
            foreach (PropertyInfo curr in p)
            {
                Console.Write(curr.Name + " ");
            }

            Console.WriteLine("");
            Console.WriteLine("");
            return;
        }

        public static bool IsList(object obj)
        {
            if (obj == null) return false;
            return obj is IList &&
                   obj.GetType().IsGenericType &&
                   obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        public static IEnumerable<T> GetRandomSample<T>(this IList<T> list, int sampleSize)
        {
            if (list == null) throw new ArgumentNullException("list");
            if (sampleSize > list.Count) throw new ArgumentException("sampleSize may not be greater than list count", "sampleSize");
            var indices = new Dictionary<int, int>(); int index;
            var rnd = new Random();

            for (int i = 0; i < sampleSize; i++)
            {
                int j = rnd.Next(i, list.Count);
                if (!indices.TryGetValue(j, out index)) index = j;

                yield return list[index];

                if (!indices.TryGetValue(i, out index)) index = i;
                indices[j] = index;
            }
        }

        #endregion

        #region Time

        public static bool IsLaterThanNow(DateTime? dt)
        {
            DateTime curr = Convert.ToDateTime(dt);
            return IsLaterThanNow(curr);
        }

        public static bool IsLaterThanNow(DateTime dt)
        {
            // less than zero: dt is earlier than now
            // equal zero: dt and now are equal
            // greater than zero: dt is later than now

            DateTime now = DateTime.Now;

            if (DateTime.Compare(dt, DateTime.Now) > 0) return true;
            return false;
        }

        #endregion

        #region String

        public static bool IsBase64(string val)
        {
            if (String.IsNullOrEmpty(val)) return false;

            try
            {
                byte[] test = Convert.FromBase64String(val);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static byte[] Base64ToBytes(string data)
        {
            try
            {
                return System.Convert.FromBase64String(data);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Base64ToUTF8(string data)
        {
            try
            {
                if (String.IsNullOrEmpty(data)) return null;
                byte[] bytes = System.Convert.FromBase64String(data);
                return System.Text.UTF8Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string BytesToBase64(byte[] data)
        {
            if (data == null) return null;
            if (data.Length < 1) return null;
            return System.Convert.ToBase64String(data);
        }

        public static string UTF8ToBase64(string data)
        {
            try
            {
                if (String.IsNullOrEmpty(data)) return null;
                byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(data);
                return System.Convert.ToBase64String(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Line(int count, string fill)
        {
            if (count < 1) return "";

            string ret = "";
            for (int i = 0; i < count; i++)
            {
                ret += fill;
            }

            return ret;
        }

        public static string Redact(string val)
        {
            if (String.IsNullOrEmpty(val)) return null;

            string redacted = "";
            for (int i = 0; i < val.Length; i++)
            {
                if ((val.Length - i) > 4)
                {
                    redacted += "*";
                }
                else
                {
                    redacted += val[i];
                }
            }

            return redacted;
        }

        public static string StringLenMax(string val, int len)
        {
            if (String.IsNullOrEmpty(val)) return null;
            if (val.Length <= len) return val;
            if (val.Length > len) return val.Substring(0, len);
            return val;
        }

        public static string StringLenFixed(string val, int len, string fillChar, bool append)
        {
            string ret = "";

            if (String.IsNullOrEmpty(val))
            {
                for (int i = 0; i < len; i++) ret += fillChar;
                return ret;
            }

            if (val.Length <= len)
            {
                if (append)
                {
                    ret = val;
                    for (int i = 0; i < (len - val.Length); i++) ret += fillChar;
                }
                else
                {
                    for (int i = 0; i < (len - val.Length); i++) ret += fillChar;
                    ret += val;
                }

                return ret;
            }

            if (val.Length > len)
            {
                return val.Substring(0, len);
            }

            return val;
        }

        public static string DynamicStringLenMax(string val, int prefixLen, int suffixLen, int fullLen)
        {
            if (String.IsNullOrEmpty(val)) return null;
            if (val.Length <= fullLen) return val;
            else if (prefixLen + suffixLen < fullLen)
                return val.Substring(0, prefixLen) + val.Substring(prefixLen, fullLen - suffixLen - prefixLen) + val.Substring((val.Length - suffixLen));
            else
                return val.Substring(0, fullLen);
        }

        public static string TitleCase(string val)
        {
            if (String.IsNullOrEmpty(val)) return null;
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(val.ToLower());
        }

        public static string RandomString(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

            string ret = "";
            int valid = 0;
            Random random = new Random();
            int num = 0;

            for (int i = 0; i < count; i++)
            {
                num = 0;
                valid = 0;
                while (valid == 0)
                {
                    num = random.Next(126);
                    if (((num > 47) && (num < 58)) ||
                        ((num > 64) && (num < 91)) ||
                        ((num > 96) && (num < 123)))
                    {
                        valid = 1;
                    }
                }
                ret += (char)num;
            }

            return ret;
        }

        public static string RandomHexString(int numHexChars)
        {
            Random random = new Random();
            byte[] buffer = new byte[numHexChars / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (numHexChars % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        public static int CharacterCountInString(string s, char c)
        {
            if (String.IsNullOrEmpty(s)) return 0;
            int count = 0;
            foreach (char curr in s)
            {
                if (curr == c) count++;
            }
            return count;
        }

        public static bool ContainsPoBox(string data)
        {
            if (String.IsNullOrEmpty(data)) return false;
            Regex rgx = new Regex("(^| +)(((box|bin)[-. \\/\\\\]?\\d+)|(p[ \\.]? ?(o|0)[-. \\/\\\\]? *-?((box( |\\n|\\.)|bin( |\\n|\\.))|b( |\\n|\\.)|(#|num)?\\d+))|(p(ost)? *(o(ff(ice)?)?)? *((box|bin)|b)? *\\d+)|(p *-?\\/?(o)? *-?box)|post office box|((box|bin)|b) *(number|num|#)? *\\d+|(num|number) *\\d+)", RegexOptions.IgnoreCase);
            return rgx.Match(data).Success;
        }

        public static string BuildIcsCalendarString(
            string recipient,
            DateTime start,
            DateTime end,
            string title,
            string description,
            string location)
        {
            string ret =
                "BEGIN:VCALENDAR\n" +
                "VERSION:2.0\n" +
                "METHOD:PUBLISH\n" +
                "PRODID:-//trainerdate//server\n" +
                "BEGIN:VEVENT\n" +
                "UID:" + recipient + "\n" +
                "DTSTAMP:" + DateTime.Now.ToString("yyyyMMddTHHmmssZ") + "\n" +
                "DTSTART:" + start.ToString("yyyyMMddTHHmm00Z") + "\n" +
                "DTEND:" + end.ToString("yyyyMMddTHHmm00Z") + "\n" +
                "SUMMARY:" + title + "\n" +
                "DESCRIPTION:" + description + "\n" +
                "LOCATION:" + location + "\n" +
                "END:VEVENT\n" +
                "END:VCALENDAR\n";

            return ret;
        }

        public static byte[] HexStringToBytes(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        #endregion

        #region Dictionary

        public static string StringValFromDict(Dictionary<string, string> dict, string key)
        {
            if (dict == null) return null;
            if (dict.Count < 1) return null;
            if (String.IsNullOrEmpty(key)) return null;

            if (dict.ContainsKey(key)) return dict[key];
            return null;
        }

        public static object ObjectValFromDict(Dictionary<string, object> dict, string key)
        {
            if (dict == null) return null;
            if (dict.Count < 1) return null;
            if (String.IsNullOrEmpty(key)) return null;

            if (dict.ContainsKey(key)) return dict[key];
            return null;
        }

        public static Dictionary<string, string> AddToDict(string key, string val, Dictionary<string, string> existing)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            if (existing == null)
            {
                ret.Add(key, val);
                return ret;
            }
            else
            {
                if (existing.ContainsKey(key))
                {
                    string tempVal = existing[key];
                    tempVal += "," + val;
                    existing.Remove(key);
                    existing.Add(key, tempVal);
                    return existing;
                }
                else
                {
                    existing.Add(key, val);
                    return existing;
                }
            }
        }

        public static Dictionary<string, object> FileToDictionary(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(filename);
            string fileContents = File.ReadAllText(filename);
            Dictionary<string, object> ret = DeserializeJson<Dictionary<string, object>>(fileContents);
            return ret;
        }

        public static Dictionary<string, string> ObjectToDictionary(object obj)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                string propName = prop.Name;
                var val = obj.GetType().GetProperty(propName).GetValue(obj, null);
                if (val != null)
                {
                    ret.Add(propName, val.ToString());
                }
            }

            return ret;
        }

        public static bool IsDictionary(object obj)
        {
            if (obj == null) return false;
            return obj is IDictionary &&
                   obj.GetType().IsGenericType &&
                   obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }

        public static T DictionaryToObject<T>(Dictionary<string, object> dict) where T : new()
        {
            string json = SerializeJson(dict);
            return DeserializeJson<T>(json);
        }

        public static string DictionaryToString(Dictionary<string, object> dict)
        {
            if (dict == null) return null;
            string ret = "";
            ret += Environment.NewLine;
            foreach (KeyValuePair<string, object> curr in dict)
            {
                if (String.IsNullOrEmpty(curr.Key)) continue;
                if (curr.Value == null) ret += "  " + curr.Key + ": (null)" + Environment.NewLine;
                else ret += "  " + curr.Key + ": " + curr.Value.ToString() + Environment.NewLine;
            }
            ret += Environment.NewLine;
            return ret;
        }

        public static Dictionary<string, object> DictAddOrReplace(Dictionary<string, object> dict, string key, object val)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (dict == null || dict.Count < 1)
            {
                Dictionary<string, object> ret = new Dictionary<string, object>();
                ret.Add(key, val);
                return ret;
            }
            else
            {
                if (dict.ContainsKey(key)) dict.Remove(key);
                dict.Add(key, val);
                return dict;
            }
        }

        public static Dictionary<string, string> DictOverwriteInsert(
            Dictionary<string, string> orig,
            string key,
            string val,
            bool insertIfNotExist,
            bool overwriteIfExists)
        {
            try
            {
                if (orig == null) return null;
                if (orig.Count < 1) return null;

                Dictionary<string, string> ret = new Dictionary<string, string>();
                bool found = false;

                foreach (KeyValuePair<string, string> curr in orig)
                {
                    if (String.Compare(curr.Key.ToLower().Trim(), key.ToLower().Trim()) == 0)
                    {
                        if (overwriteIfExists)
                        {
                            ret.Add(curr.Key, val);
                            found = true;
                        }
                        else
                        {
                            ret.Add(curr.Key, curr.Value);
                        }
                    }
                    else
                    {
                        ret.Add(curr.Key, curr.Value);
                    }
                }

                if (insertIfNotExist)
                {
                    if (!found)
                    {
                        ret.Add(key, val);
                    }
                }

                return ret;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region IsTrue

        public static bool IsTrue(int? val)
        {
            if (val == null) return false;
            if (Convert.ToInt32(val) == 1) return true;
            return false;
        }

        public static bool IsTrue(int val)
        {
            if (val == 1) return true;
            return false;
        }

        public static bool IsTrue(bool val)
        {
            return val;
        }

        public static bool IsTrue(bool? val)
        {
            if (val == null) return false;
            return Convert.ToBoolean(val);
        }

        public static bool IsTrue(string val)
        {
            if (String.IsNullOrEmpty(val)) return false;
            val = val.ToLower().Trim();
            int valInt = 0;
            if (Int32.TryParse(val, out valInt)) if (valInt == 1) return true;
            if (String.Compare(val, "true") == 0) return true;
            return false;
        }

        #endregion
        
        #region Input

        public static bool InputBoolean(string question, bool yesDefault)
        {
            Console.Write(question);

            if (yesDefault) Console.Write(" [Y/n]? ");
            else Console.Write(" [y/N]? ");

            string userInput = Console.ReadLine();

            if (String.IsNullOrEmpty(userInput))
            {
                if (yesDefault) return true;
                return false;
            }

            userInput = userInput.ToLower();

            if (yesDefault)
            {
                if (
                    (String.Compare(userInput, "n") == 0)
                    || (String.Compare(userInput, "no") == 0)
                   )
                {
                    return false;
                }

                return true;
            }
            else
            {
                if (
                    (String.Compare(userInput, "y") == 0)
                    || (String.Compare(userInput, "yes") == 0)
                   )
                {
                    return true;
                }

                return false;
            }
        }

        public static string InputString(string question, string defaultAnswer, bool allowNull)
        {
            while (true)
            {
                Console.Write(question);

                if (!String.IsNullOrEmpty(defaultAnswer))
                {
                    Console.Write(" [" + defaultAnswer + "]");
                }

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                    if (allowNull) return null;
                    else continue;
                }

                return userInput;
            }
        }

        public static int InputInteger(string question, int defaultAnswer, bool positiveOnly, bool allowZero)
        {
            while (true)
            {
                Console.Write(question);
                Console.Write(" [" + defaultAnswer + "] ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    return defaultAnswer;
                }

                int ret = 0;
                if (!Int32.TryParse(userInput, out ret))
                {
                    Console.WriteLine("Please enter a valid integer.");
                    continue;
                }

                if (ret == 0)
                {
                    if (allowZero)
                    {
                        return 0;
                    }
                }

                if (ret < 0)
                {
                    if (positiveOnly)
                    {
                        Console.WriteLine("Please enter a value greater than zero.");
                        continue;
                    }
                }

                return ret;
            }
        }

        #endregion

        #region Directory

        public static bool VerifyDirectoryAccess(string directory)
        {
            try
            {
                if (String.IsNullOrEmpty(directory)) return false;
                DirectorySecurity ds = Directory.GetAccessControl(directory);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool CreateDirectory(string dir)
        {
            Directory.CreateDirectory(dir);
            return true;
        }

        public static bool DirectoryExists(string dir)
        {
            try
            {
                return Directory.Exists(dir);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static List<string> GetSubdirectoryList(string directory, bool recursive)
        {
            try
            {
                /*
                 * Prepends the 'directory' variable to the name of each directory already
                 * so each is immediately usable from the resultant list
                 * 
                 * Does NOT append a slash
                 * Does NOT include the original directory in the list
                 * Does NOT include child files
                 * 
                 * i.e. 
                 * C:\code\kvpbase
                 * C:\code\kvpbase\src
                 * C:\code\kvpbase\test
                 * 
                 */

                string[] folders;

                if (recursive)
                {
                    folders = Directory.GetDirectories(@directory, "*", SearchOption.AllDirectories);
                }
                else
                {
                    folders = Directory.GetDirectories(@directory, "*", SearchOption.TopDirectoryOnly);
                }

                List<string> folderList = new List<string>();

                foreach (string folder in folders)
                {
                    folderList.Add(folder);
                }

                return folderList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool DeleteDirectory(string dir, bool recursive)
        {
            try
            {
                Directory.Delete(dir, recursive);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool RenameDirectory(string from, string to)
        {
            try
            {
                if (String.IsNullOrEmpty(from)) return false;
                if (String.IsNullOrEmpty(to)) return false;
                if (String.Compare(from, to) == 0) return true;
                Directory.Move(from, to);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool MoveDirectory(string from, string to)
        {
            try
            {
                if (String.IsNullOrEmpty(from)) return false;
                if (String.IsNullOrEmpty(to)) return false;
                if (String.Compare(from, to) == 0) return true;
                Directory.Move(from, to);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
         
        public static bool DirectoryStatistics(
            DirectoryInfo dirinfo,
            bool recursive,
            out long bytes,
            out int files,
            out int subdirs)
        {
            bytes = 0;
            files = 0;
            subdirs = 0;

            try
            {
                FileInfo[] fis = dirinfo.GetFiles();
                files = fis.Length;

                foreach (FileInfo fi in fis)
                {
                    bytes += fi.Length;
                }

                // Add subdirectory sizes
                DirectoryInfo[] subdirinfos = dirinfo.GetDirectories();

                if (recursive)
                {
                    foreach (DirectoryInfo subdirinfo in subdirinfos)
                    {
                        subdirs++;
                        long subdirBytes = 0;
                        int subdirFiles = 0;
                        int subdirSubdirectories = 0;

                        if (Common.DirectoryStatistics(subdirinfo, recursive, out subdirBytes, out subdirFiles, out subdirSubdirectories))
                        {
                            bytes += subdirBytes;
                            files += subdirFiles;
                            subdirs += subdirSubdirectories;
                        }
                    }
                }
                else
                {
                    subdirs = subdirinfos.Length;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region File

        public static bool DeleteFile(string filename)
        {
            try
            {
                File.Delete(@filename);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool FileExists(string filename)
        {
            return File.Exists(filename);
        }

        public static bool VerifyFileReadAccess(string filename)
        {
            try
            {
                using (FileStream stream = File.Open(filename, System.IO.FileMode.Open, FileAccess.Read))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }
         
        public static bool WriteFile(string filename, string content, bool append)
        {
            using (StreamWriter writer = new StreamWriter(filename, append))
            {
                writer.WriteLine(content);
            }
            return true;
        }

        public static bool WriteFile(string filename, byte[] data)
        {
            // File.WriteAllBytes(filename, data);
            using (var fs = new FileStream(
                filename,
                System.IO.FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                0x1000,
                FileOptions.WriteThrough))
            {
                fs.Write(data, 0, data.Length);
            }
            return true;
        }

        public static bool WriteFile(string filename, byte[] content, int pos)
        {
            using (Stream stream = new FileStream(filename, System.IO.FileMode.OpenOrCreate))
            {
                stream.Seek(pos, SeekOrigin.Begin);
                stream.Write(content, 0, content.Length);
            }
            return true;
        }

        public static string ReadTextFile(string filename)
        {
            return File.ReadAllText(@filename);
        }

        public static byte[] ReadBinaryFile(string filename, int from, int len)
        {
            if (len < 1) return null;
            if (from < 0) return null;

            byte[] ret = new byte[len];
            using (BinaryReader reader = new BinaryReader(new FileStream(filename, System.IO.FileMode.Open)))
            {
                reader.BaseStream.Seek(from, SeekOrigin.Begin);
                reader.Read(ret, 0, len);
            }

            return ret;
        }

        public static byte[] ReadBinaryFile(string filename)
        {
            return File.ReadAllBytes(@filename);
        }

        public static string GetFileExtension(string filename)
        {
            try
            {
                if (String.IsNullOrEmpty(filename)) return null;
                return Path.GetExtension(filename);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool RenameFile(string from, string to)
        {
            try
            {
                if (String.IsNullOrEmpty(from)) return false;
                if (String.IsNullOrEmpty(to)) return false;

                if (String.Compare(from, to) == 0) return true;
                File.Move(from, to);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool MoveFile(string from, string to)
        {
            try
            {
                if (String.IsNullOrEmpty(from)) return false;
                if (String.IsNullOrEmpty(to)) return false;

                if (String.Compare(from, to) == 0) return true;
                File.Move(from, to);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HideFile(string filename)
        {
            try
            {
                File.SetAttributes(filename, File.GetAttributes(filename) | FileAttributes.Hidden);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SetBetInFile(string filename, int bitnum, bool val)
        {
            int bytenum = bitnum / 8;
            byte[] ba = Common.ReadBinaryFile(filename, bytenum, 1);
            int bitpos = bitnum % 8;

            byte mask = (byte)(1 << bitpos);
            bool isSet = (ba[0] & mask) != 0;

            if (!val)
            {
                ba[0] &= mask;
            }
            else
            {
                ba[0] |= mask;
            }

            using (Stream stream = new FileStream(filename, System.IO.FileMode.OpenOrCreate))
            {
                stream.Seek(bytenum, SeekOrigin.Begin);
                stream.Write(ba, 0, ba.Length);
            }

            return true;
        }

        public static bool GetBitFromFile(string filename, int bitnum, out bool val)
        {
            val = false;

            int bytenum = bitnum / 8;
            byte[] ba = Common.ReadBinaryFile(filename, bytenum, 1);
            byte data = ba[0];

            int bitpos = bitnum % 8;

            val = (data & (1 << bitpos)) != 0;
            return true;
        }

        #endregion

        #region Crypto

        public static string Md5(byte[] data)
        {
            if (data == null) return null;
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(data);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
            string ret = sb.ToString();
            return ret;
        }

        public static string Md5(string data)
        {
            if (String.IsNullOrEmpty(data)) return null;
            MD5 md5 = MD5.Create();
            byte[] dataBytes = System.Text.Encoding.ASCII.GetBytes(data);
            byte[] hash = md5.ComputeHash(dataBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
            string ret = sb.ToString();
            return ret;
        }

        public static byte[] Sha1(byte[] data)
        {
            if (data == null || data.Length < 1) return null;
            SHA1Managed s = new SHA1Managed();
            return s.ComputeHash(data);
        }

        public static byte[] Sha256(byte[] data)
        {
            if (data == null || data.Length < 1) return null;
            SHA256Managed s = new SHA256Managed();
            return s.ComputeHash(data);
        }

        #endregion

        #endregion

        #region Private-Methods

        #endregion
    }
}
