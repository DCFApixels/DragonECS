using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace DCFApixels.DragonECS.Docs
{
    [Serializable]
    [DataContract]
    public class DragonDocs : ISerializable
    {
        [NonSerialized]
        private List<DragonDocsMeta> _metas = new List<DragonDocsMeta>();
        [DataMember]
        private SortedDictionary<string, List<DragonDocsMeta>> _groups = new SortedDictionary<string, List<DragonDocsMeta>>();

        protected DragonDocs(SerializationInfo info, StreamingContext context)
        {

        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
        private DragonDocs() { }
        private DragonDocs(List<DragonDocsMeta> metaList)
        {
            _metas = metaList;
            foreach (var docsMeta in _metas)
            {
                string group = docsMeta.Group;
                if (_groups.TryGetValue(group, out List<DragonDocsMeta> groupMetas) == false)
                {
                    groupMetas = new List<DragonDocsMeta>();
                    _groups.Add(group, groupMetas);
                }
                groupMetas.Add(docsMeta);
            }
        }

        public static DragonDocs Generate()
        {
            DragonDocs result = new DragonDocs();
            List<Type> types = GetTypes();
            foreach (var type in types)
            {
                TypeMeta meta = type.ToMeta();
                DragonDocsMeta docsMeta = new DragonDocsMeta(meta);
                result._metas.Add(docsMeta);
                string group = meta.Group.Name;

                if (result._groups.TryGetValue(group, out List<DragonDocsMeta> groupMetas) == false)
                {
                    groupMetas = new List<DragonDocsMeta>();
                    result._groups.Add(group, groupMetas);
                }

                groupMetas.Add(docsMeta);
            }

            return result;
        }
        private static List<Type> GetTypes()
        {
            Type ecsMetaAttributeType = typeof(EcsMetaAttribute);
            List<Type> result = new List<Type>();
            Type memberType = typeof(IEcsMember);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (memberType.IsAssignableFrom(type) || Attribute.GetCustomAttributes(type, ecsMetaAttributeType, false).Length > 1)
                    {
                        result.Add(type);
                    }
                }
            }
            return result;
        }



        #region Serialization
        public string SerializeToJson()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{ \"data\": [\r");
            foreach (var group in _groups)
            {
                //json.Append($"\"{group.Key}\": ");
                json.Append($"{{ \"key\": \"{group.Key}\",\r\"values\": [");
                foreach (var meta in group.Value)
                {
                    meta.SerializeToJson(json);
                }
                json.Append("] }, \r");
            }
            json.Append(" ] }");
            return json.ToString();
        }



        private static DragonDocs CreateDocs(ref JsonIterator reader)
        {
            JsonNode node;
            List<DragonDocsMeta> metas = new List<DragonDocsMeta>();
            while (reader.Next())
            {
                node = reader.Next();
                if (node.IsKey && node.EqualsString("data") && reader.Next().IsEnter)
                {
                    while (reader.Next().IsEnter && (node = reader.Next()).IsKey && node.EqualsString("key"))
                    {
                        node = reader.Next();
                          string group = node.ToString();
                        node = reader.Next();
                        if (node.IsKey && node.EqualsString("values") && reader.Next().IsEnter)
                        {
                            while (reader.Next().IsEnter)
                            {
                                metas.Add(DragonDocsMeta.DeserializeFromJson(ref reader, group));
                            }
                        }
                        if(reader.Next().IsExit == false)
                        {
                            throw new Exception();
                        }
                    }
                    return new DragonDocs(metas);
                }
                else
                {
                    throw new Exception();
                }
            }
            throw new Exception();
        }
        public unsafe static DragonDocs DeserializeFromJson(string jsonString)
        {
            CB.Set(jsonString);
            fixed (char* ptr = jsonString)
            {
                JsonIterator reader = new JsonIterator(ptr, jsonString.Length);
                return CreateDocs(ref reader);
            }
            throw new Exception();




            //fixed (char* ptr = jsonString)
            //{
            //    //var result = DragonDocsJsonReader.ReadJson(ptr, jsonString.Length);
            //
            //
            //}
            //
            //throw new NotImplementedException();

            //jsonString = jsonString.Replace("{", "").Replace("}", "").Replace("\"", "");

            // Разбиваем JSON строку на пары ключ-значение
            //string[] parts = jsonString.Split(new string[] { "key:", "values:[" }, StringSplitOptions.RemoveEmptyEntries);



            //string[] parts = jsonString.Split("\"key\":", StringSplitOptions.RemoveEmptyEntries);
            //List<DragonDocsMeta> metas = new List<DragonDocsMeta>();
            //
            //
            //for (int i = 1; i < parts.Length; i += 2)
            //{
            //    string key = parts[i];
            //    int indexOf = key.IndexOf('"');
            //    key = key.Substring(indexOf + 1, key.IndexOf('"', indexOf + 1) - indexOf - 1);
            //    string values = parts[i + 1];
            //
            //    Match match = Regex.Match(values, @"""values"": \[(.*?)\].*");
            //    if (match.Success)
            //    {
            //        values = match.Groups[1].Value;
            //    }
            //
            //    CB.Set(values);
            //
            //
            //    string[] metaValues = values.Split(new string[] { "},{" }, StringSplitOptions.None);
            //
            //    foreach (string metaValue in metaValues)
            //    {
            //        // Десериализация отдельного метаданных (нужно реализовать метод DeserializeFromJson в DragonDocsMeta)
            //        DragonDocsMeta meta = DragonDocsMeta.DeserializeFromJson("{" + metaValue + "}", key);
            //        metas.Add(meta);
            //    }
            //}
            //return new DragonDocs(metas);




            // Удаляем лишние символы из JSON строки
            //json = json.Replace("{", "").Replace("}", "").Replace("\"", "");
            //
            //// Разбиваем JSON строку на пары ключ-значение
            //string[] pairs = json.Split(',');
            //
            //// Создаем переменные для хранения текущих значений
            //string currentKey = "";
            //List<DragonDocsMeta> currentMetas = new List<DragonDocsMeta>();
            //
            //foreach (string pair in pairs)
            //{
            //    string[] keyValue = pair.Split(':');
            //
            //    if (keyValue.Length == 2)
            //    {
            //        string key = keyValue[0].Trim();
            //        string value = keyValue[1].Trim();
            //
            //        if (key == "key")
            //        {
            //            currentKey = value;
            //        }
            //        else if (key == "values")
            //        {
            //            // Очищаем список метаданных перед добавлением новых
            //            currentMetas.Clear();
            //
            //            // Разбиваем значение на отдельные метаданные и десериализуем каждое из них
            //            string[] metaValues = value.Split(new string[] { "},{" }, StringSplitOptions.None);
            //            foreach (string metaValue in metaValues)
            //            {
            //                // Десериализация отдельного метаданных (нужно реализовать метод DeserializeFromJson в DragonDocsMeta)
            //                DragonDocsMeta meta = DragonDocsMeta.DeserializeFromJson("{" + metaValue + "}", currentKey);
            //                currentMetas.Add(meta);
            //            }
            //        }
            //    }
            //}
            //return new DragonDocs(currentMetas);
        }



        //public static DragonDocs DeserializeFromJson(string jsonString)
        //{
        //    //jsonString = jsonString.Replace("{", "").Replace("}", "").Replace("\"", "");
        //
        //    // Разбиваем JSON строку на пары ключ-значение
        //    //string[] parts = jsonString.Split(new string[] { "key:", "values:[" }, StringSplitOptions.RemoveEmptyEntries);
        //    string[] parts = jsonString.Split("\"key\":", StringSplitOptions.RemoveEmptyEntries);
        //    List<DragonDocsMeta> metas = new List<DragonDocsMeta>();
        //
        //
        //    for (int i = 1; i < parts.Length; i += 2)
        //    {
        //        string key = parts[i];
        //        int indexOf = key.IndexOf('"');
        //        key = key.Substring(indexOf + 1, key.IndexOf('"', indexOf + 1) - indexOf - 1);
        //        string values = parts[i + 1];
        //
        //        Match match = Regex.Match(values, @"""values"": \[(.*?)\].*");
        //        if (match.Success)
        //        {
        //            values = match.Groups[1].Value;
        //        }
        //
        //        CB.Set(values);
        //
        //
        //        string[] metaValues = values.Split(new string[] { "},{" }, StringSplitOptions.None);
        //
        //        foreach (string metaValue in metaValues)
        //        {
        //            // Десериализация отдельного метаданных (нужно реализовать метод DeserializeFromJson в DragonDocsMeta)
        //            DragonDocsMeta meta = DragonDocsMeta.DeserializeFromJson("{" + metaValue + "}", key);
        //            metas.Add(meta);
        //        }
        //    }
        //    return new DragonDocs(metas);
        //
        //}
        #endregion
    }
}