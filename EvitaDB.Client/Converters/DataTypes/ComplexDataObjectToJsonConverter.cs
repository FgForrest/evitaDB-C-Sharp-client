using System.Globalization;
using Client.DataTypes;
using Client.DataTypes.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Client.Converters.DataTypes;

public class ComplexDataObjectToJsonConverter : IDataItemVisitor
{
    private readonly JsonSerializerSettings _settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };

    private readonly Stack<string> _propertyNameStack = new();
    private readonly Stack<JToken> _stack = new();
    public JToken? RootNode { get; private set; }

    public void Visit(DataItemArray arrayItem)
    {
        if (RootNode == null)
        {
            RootNode = new JArray();
            _stack.Push(RootNode);
        }
        else
        {
            var stackNode = _stack.Peek();
            if (stackNode is JObject objectNode)
            {
                objectNode.Add(_propertyNameStack.Peek(), new JArray());
                _stack.Push(objectNode);
            }
            else if (stackNode is JArray arrayNode)
            {
                JArray newArray = new();
                arrayNode.Add(newArray);
                _stack.Push(newArray);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected node on stack: {stackNode}");
            }
        }

        arrayItem.ForEach((dataItem, hasNext) =>
        {
            if (dataItem is null)
            {
                WriteNull();
            }
            else
            {
                dataItem.Accept(this);
            }
        });

        _stack.Pop();
    }

    public void Visit(DataItemMap mapItem)
    {
        if (RootNode == null)
        {
            RootNode = new JObject();
            _stack.Push(RootNode);
        }
        else
        {
            var stackNode = _stack.Peek();
            if (stackNode is JObject objectNode)
            {
                objectNode.Add(_propertyNameStack.Peek());
                _stack.Push(objectNode);
            }
            else if (stackNode is JArray arrayNode)
            {
                JObject newObject = new();
                arrayNode.Add(newObject);
                _stack.Push(newObject);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected node on stack: {stackNode}");
            }
        }

        mapItem.ForEach((propertyName, dataItem, hasNext) =>
        {
            _propertyNameStack.Push(propertyName);

            if (dataItem == null)
            {
                WriteNull();
            }
            else
            {
                dataItem.Accept(this);
            }

            _propertyNameStack.Pop();
        });

        _stack.Pop();
    }

    public void Visit(DataItemValue valueItem)
    {
        if (RootNode == null)
        {
            throw new InvalidOperationException("Value item is not allowed as the root item!");
        }

        var theNode = _stack.Peek();

        if (theNode is JObject objectNode)
        {
            var propertyName = _propertyNameStack.Peek();
            var value = valueItem.Value;

            switch (value)
            {
                case short s:
                    objectNode.Add(propertyName, s);
                    break;
                case byte b:
                    objectNode.Add(propertyName, b);
                    break;
                case int i:
                    objectNode.Add(propertyName, i);
                    break;
                case long l:
                    objectNode.Add(propertyName, l.ToString());
                    break;
                case string s:
                    objectNode.Add(propertyName, s);
                    break;
                case decimal d:
                    objectNode.Add(propertyName, d.ToString(CultureInfo.InvariantCulture));
                    break;
                case bool b:
                    objectNode.Add(propertyName, b);
                    break;
                case char c:
                    objectNode.Add(propertyName, c.ToString());
                    break;
                case CultureInfo cultureInfo:
                    objectNode.Add(propertyName, cultureInfo.Name);
                    break;
                case null:
                    objectNode.Add(propertyName, JValue.CreateNull());
                    break;
                default:
                    objectNode.Add(propertyName, EvitaDataTypes.FormatValue(value));
                    break;
            }
        }
        else if (theNode is JArray arrayNode)
        {
            var value = valueItem.Value;

            switch (value)
            {
                case short s:
                    arrayNode.Add(s);
                    break;
                case byte b:
                    arrayNode.Add(b);
                    break;
                case int i:
                    arrayNode.Add(i);
                    break;
                case long l:
                    arrayNode.Add(l.ToString());
                    break;
                case string s:
                    arrayNode.Add(s);
                    break;
                case decimal d:
                    arrayNode.Add(d.ToString(CultureInfo.InvariantCulture));
                    break;
                case bool b:
                    arrayNode.Add(b);
                    break;
                case char c:
                    arrayNode.Add(c.ToString());
                    break;
                case CultureInfo cultureInfo:
                    arrayNode.Add(cultureInfo.Name);
                    break;
                case null:
                    arrayNode.Add(JValue.CreateNull());
                    break;
                default:
                    arrayNode.Add(EvitaDataTypes.FormatValue(value));
                    break;
            }
        }
        else
        {
            throw new InvalidOperationException($"Unexpected type of node on stack: {theNode?.GetType()}");
        }
    }

    public string GetJsonAsString()
    {
        return JsonConvert.SerializeObject(RootNode, _settings);
    }

    private void WriteNull()
    {
        if (RootNode == null)
        {
            RootNode = new JObject();
            _stack.Push(RootNode);
        }

        var theNode = _stack.Peek();

        if (theNode is JObject objectNode)
        {
            var propertyName = _propertyNameStack.Peek();
            objectNode.Add(propertyName, JValue.CreateNull());
        }
        else if (theNode is JArray arrayNode)
        {
            arrayNode.Add(JValue.CreateNull());
        }
        else
        {
            throw new InvalidOperationException($"Unexpected type of node on stack: {theNode?.GetType()}");
        }
    }
}