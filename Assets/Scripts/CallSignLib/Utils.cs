namespace CallSignLib
{

using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public static class Utils
{
    public static string SerializeToXml<T>(T obj)
    {
        // TODO: cache
        XmlSerializer ser = new XmlSerializer(typeof(T));
        using(var textWriter = new StringWriter())
        {
            using(var xmlWriter = XmlWriter.Create(textWriter))
            {
                ser.Serialize(xmlWriter, obj);
                string serializedXml = textWriter.ToString();

                return serializedXml;
            }
        }
    }

    public static T DeserializeFromXml<T>(string serializedXml)
    {
        // TODO: cache
        XmlSerializer ser = new XmlSerializer(typeof(T));

        using(var textReader = new StringReader(serializedXml))
        {
            using(var xmlReader = XmlReader.Create(textReader))
            {
                T obj = (T)ser.Deserialize(xmlReader);

                return obj;
            }
        }
    }

    static Random random = new();

    public static int D6()
    {
        return random.Next(1, 6);
    }

    public static (int, int) D6Compare()
    {
        while(true)
        {
            var r1 = D6();
            var r2 = D6();
            if(r1 != r2)
            {
                return (r1, r2);
            }
        }
    }

    public static List<List<T>> CartesianProduct<T>(List<List<T>> records)
    {
        if(records.Count == 0)
        {
            return new();
        }
        if(records.Count == 1)
        {
            var record = records[0];
            return record.Select(r => new List<T>(){r}).ToList();
            // return records;
        }

        var rets = new List<List<T>>();
        var tail = CartesianProduct(records.Skip(1).ToList());
        foreach(var head in records[0])
        {
            foreach(var t in tail)
            {
                var l = new List<T>(){head};
                l.AddRange(t);
                rets.Add(l);
            }
        }
        return rets;
    }

    public static string ToStr(float[] arr)
    {
        return string.Join(" ", arr);
    }
}

}