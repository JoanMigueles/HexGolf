using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVExporter
{

    private string filePath;

    public CSVExporter()
    {
        filePath = Application.dataPath + "/golfData.csv";

        // Initialize CSV file with headers
        InitializeCSV();
    }

    private void InitializeCSV()
    {
        if (!File.Exists(filePath)) {
            using (StreamWriter sw = new StreamWriter(filePath)) {
                sw.WriteLine("Player, Hits, Win, Path length");
            }
        }
    }

    public void ExportToCSV(int player, int hits, bool win, int length)
    {
        string playerType;
        if (player == 0) { playerType = "Human"; }
        else { playerType = "AI"; };
        // Prepare data row
        string dataRow = $"{playerType},{hits},{win},{length}";

        // Append data to the file
        using (StreamWriter sw = new StreamWriter(filePath, true)) {
            sw.WriteLine(dataRow);
        }

        Debug.Log("Data exported to CSV: " + filePath);
    }
}