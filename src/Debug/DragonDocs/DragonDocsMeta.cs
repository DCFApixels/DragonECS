using DCFApixels.DragonECS.Internal;
using System;
using System.Text;

namespace DCFApixels.DragonECS.Docs
{
    public class DragonDocsMeta
    {
        private Type _sourceType;
        private bool _isInitSourceType = false;
        public readonly string AssemblyQualifiedName = string.Empty;

        public readonly string Name = string.Empty;
        public readonly bool IsCustomName = false;
        public readonly MetaColor Color = MetaColor.BlackColor;
        public readonly bool IsCustomColor = false;
        public readonly string Autor = string.Empty;
        public readonly string Description = string.Empty;

        public readonly string Group = string.Empty;
        public readonly string Tags = string.Empty;

        public DragonDocsMeta(TypeMeta meta)
        {
            _sourceType = meta.Type;
            AssemblyQualifiedName = meta.Type.AssemblyQualifiedName;

            Name = meta.Name;
            IsCustomName = meta.IsCustomName;
            Color = meta.Color;
            IsCustomColor = meta.IsCustomColor;
            Autor = meta.Description.Author;
            Description = meta.Description.Text;

            Group = meta.Group.Name;
            Tags = string.Join(", ", meta.Tags);
        }

        public bool TryGetSourceType(out Type type)
        {
            type = GetSourceType();
            return type != null;
        }
        private Type GetSourceType()
        {
            if (_isInitSourceType) { return _sourceType; }
            _isInitSourceType = true;
            _sourceType = Type.GetType(AssemblyQualifiedName);
            return _sourceType;
        }

        #region Serialization
        public void SerializeToJson(StringBuilder json)
        {
            json.Append("{");

            SerializeFieldToJson(json, nameof(AssemblyQualifiedName), AssemblyQualifiedName, true, true);
            SerializeFieldToJson(json, nameof(Name), Name, true, true);
            SerializeFieldToJson(json, nameof(IsCustomName), IsCustomName.ToString().ToLower(), false, true);
            SerializeFieldToJson(json, nameof(Color), Color.ToString(), true, true);
            SerializeFieldToJson(json, nameof(IsCustomColor), IsCustomColor.ToString().ToLower(), false, true);

            SerializeFieldToJson(json, nameof(Autor), Autor, true, false);
            SerializeFieldToJson(json, nameof(Description), Description, true, false);
            //SerializeFieldToJson(json, nameof(Group), Group, false);
            SerializeFieldToJson(json, nameof(Tags), Tags, true, false);

            json.Remove(json.Length - 1, 1);
            json.Append("}");
        }
        private void SerializeFieldToJson(StringBuilder json, string fieldName, string value, bool isString, bool alwaysSerialize)
        {
            if (alwaysSerialize || !string.IsNullOrEmpty(value))
            {
                json.Append($"\"{fieldName}\":");
                if (isString)
                {
                    json.Append($"\"{value}\"");
                }
                else
                {
                    json.Append($"{value}");
                }
                json.Append(",");
            }
        }


        internal static DragonDocsMeta DeserializeFromJson(ref JsonIterator reader, string group)
        {
            return new DragonDocsMeta(ref reader, group);
        }
        private unsafe DragonDocsMeta(ref JsonIterator reader, string group)
        {
            Group = group;
            JsonNode node;
            int depth = reader.Depth;
            while ((node = reader.Next()).Depth >= depth)
            {
                //TODO тут можно сделать систему которая бы учитывала что порядок полей может меняться по мере разработки.
                //Есть идея через рефлекшн разбирать состояние типа, после создавать массив индексов,
                //а эти индексы потом могли бы применяться в switch-е в котором кейсы на каждый индекс по порядку
                if (node.IsKey && node.EqualsString(nameof(AssemblyQualifiedName)))
                {
                    AssemblyQualifiedName = reader.Next().ToString();
                    if ((node = reader.Next()).Depth < depth) { break; }
                }
                if (node.IsKey && node.EqualsString(nameof(Name)))
                {
                    Name = reader.Next().ToString();
                    if ((node = reader.Next()).Depth < depth) { break; }
                }
                if (node.IsKey && node.EqualsString(nameof(IsCustomName)))
                {
                    IsCustomName = UnsafeBoolParse(reader.Next().Ptr);
                    if ((node = reader.Next()).Depth < depth) { break; }
                }
                if (node.IsKey && node.EqualsString(nameof(Color)))
                {
                    node = reader.Next();
                    Color = MetaColor.Parse(node.Ptr, node.Length);
                    if ((node = reader.Next()).Depth < depth) { break; }
                }
                if (node.IsKey && node.EqualsString(nameof(IsCustomColor)))
                {
                    IsCustomColor = UnsafeBoolParse(reader.Next().Ptr);
                    if ((node = reader.Next()).Depth < depth) { break; }
                }

                if (node.IsKey && node.EqualsString(nameof(Autor)))
                {
                    Autor = reader.Next().ToString();
                    if ((node = reader.Next()).Depth < depth) { break; }
                }
                if (node.IsKey && node.EqualsString(nameof(Description)))
                {
                    Description = reader.Next().ToString();
                    if ((node = reader.Next()).Depth < depth) { break; }
                }
                //if (node.IsKey && node.EqualsString(nameof(Group)))
                //{
                //    Group = reader.Next().ToString();
                //    if ((node = reader.Next()).Depth < depth) { break; }
                //}
                if (node.IsKey && node.EqualsString(nameof(Tags)))
                {
                    Tags = reader.Next().ToString();
                    if ((node = reader.Next()).Depth < depth) { break; }
                }
            }
        }
        private static unsafe bool UnsafeBoolParse(char* ptr)
        {
            return char.ToLower(ptr[0]) == 't';
        }



        public static DragonDocsMeta DeserializeFromJson(string json, string currentGroup)
        {
            return new DragonDocsMeta(json, currentGroup);
        }
        /// <summary> DeserializeFromJson constructor. </summary>
        private DragonDocsMeta(string json, string currentGroup)
        {
            // Удаляем лишние символы из JSON строки
            json = json.Replace("{", "").Replace("}", "").Replace("\"", "");
            // Разбиваем JSON строку на пары ключ-значение
            string[] pairs = json.Split(',');

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split(':');

                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    switch (key)
                    {
                        case nameof(AssemblyQualifiedName):
                            AssemblyQualifiedName = value;
                            break;
                        case nameof(Name):
                            Name = value;
                            break;
                        case nameof(IsCustomName):
                            IsCustomName = bool.Parse(value);
                            break;
                        case nameof(Color):
                            Color = MetaColor.Parse(value);
                            break;
                        case nameof(IsCustomColor):
                            IsCustomColor = bool.Parse(value);
                            break;
                        case nameof(Autor):
                            Autor = value;
                            break;
                        case nameof(Description):
                            Description = value;
                            break;
                        case nameof(Tags):
                            Tags = value;
                            break;
                        default: break;
                    }
                }
            }
            Group = currentGroup;
        }
        #endregion
    }
}