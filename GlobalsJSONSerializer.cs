using Klaes.Construction;
using Klaes.Framework;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GlobalsSerializer;

/// <summary>
/// Klasse zum Serialisieren von Globalen Variablen und Records (LSP etc.) in JSON-Dateien zum Vergleichen und Auswerten von deren Inhalt
/// Die Dateien werden in den Mandanten-Daten Ordner geschrieben.
/// </summary>
public static class GlobalsJSONSerializer
{
    private static string DataFolderPath { get; set; } = string.Format("{0}/Konstruktion_Serialization/", SingletonBase<SystemSettings, Lazy<SystemSettings>>.Instance.DataPath);
    private const string m_SolutionName = "Klaes.Construction";

    /// <summary>
    /// Löscht alle Dateien in dem Output-Ordner
    /// </summary>
    public static void ClearDataFolder()
    {
        if (!Directory.Exists(DataFolderPath))
            return;

        foreach (string lsDeleteFile in Directory.GetFiles(DataFolderPath, "*.*", SearchOption.TopDirectoryOnly))
            File.Delete(lsDeleteFile);
    }

    /// <summary>
    /// Serialisiert alle Felder aus den üblichen Modulen mit globalen Variablen
    /// </summary>
    public static void SerializeGlobalVariables()
    {
        SerializeModule("publicaw");
        SerializeModule("Publicg");
        SerializeModule("Public50");
        SerializeModule("Public60");
    }

    /// <summary>
    /// Serialisiert ein Object in JSON und schreibt es in den Mandanten-Daten-Ordner
    /// </summary>
    /// <typeparam name="T">Typ des Objektes</typeparam>
    /// <param name="ptObject">Zu serialisierendes Objekt</param>
    /// <param name="psFileName">Dateiname zur Ausgabe</param>
    public static void SerializeObject<T>(T ptObject, string psFileName)
    {
        try
        {
            FileInfo ltFileName = new(string.Format("{0}{1}.json", DataFolderPath, psFileName));
            Directory.CreateDirectory(ltFileName.DirectoryName);

            using TextWriter ltWriter = new StreamWriter(ltFileName.FullName);
            string lsJSON = JsonConvert.SerializeObject(ptObject, Formatting.Indented);
            ltWriter.Write(lsJSON);
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.Message);
        }
    }

    /// <summary>
    /// Serialisiert alle Felder des übergebenen Moduls
    /// </summary>
    /// <param name="psModuleName">Name des zu serialisierenden Moduls</param>
    private static void SerializeModule(string psModuleName)
    {
        Assembly[] ltAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.Contains(m_SolutionName)).ToArray();

        Type ltModule = null;
        foreach (Assembly ltAssembly in ltAssemblies)
        {
            ltModule = ltAssembly.GetType(psModuleName);
            if (ltModule is not null)
                break;
        }

        FieldInfo[] ltFields = ltModule?.GetFields();
        foreach (FieldInfo ltField in ltFields)
        {
            string lsFileName = string.Format("{0}_{1}_{2}", psModuleName, ltField.Name, ltField.FieldType.Name);
            SerializeObject(ltField.GetValue(ltField), lsFileName);
        }
    }
}