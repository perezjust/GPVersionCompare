using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using ESRI.ArcGIS.esriSystem;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.ADF.CATIDs;

namespace GPVersionDifferences
{
    class Program
    {
        private static LicenseInitializer m_AOLicenseInitializer = new GPVersionDifferences.LicenseInitializer();
        

        [STAThread()]
        static void Main(string[] args)
        {
            //ESRI License Initializer generated code.
            m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeStandard },
            new esriLicenseExtensionCode[] { });
            //ESRI License Initializer generated code.
            //Do not make any call to ArcObjects after ShutDownApplication()

            IGeoProcessor2 gp = new GeoProcessorClass();
            //GPListFeatureClasses(gp);

            if (args.Length == 0)
            {
                args = new string[] { "pods.GIS.Tank", @"C:\geoproc\DataQualityTools\VersionDiff\pods_os.sde", Directory.GetCurrentDirectory() + "\\diff_folder", "DATAQUALITY.DataQuality", "sde.Default" };
            }

            string diffObject = args[0];
            string database = args[1];
            string diffFolder = args[2];
            string versionTargetInput = args[3];
            string versionCompare = args[4];

            string[] versionTargetArray = versionTargetInput.Split(new string[] { "." }, StringSplitOptions.None);
            string versionTarget = "\"" + versionTargetArray.GetValue(0).ToString() + "\"" + "." + versionTargetArray.GetValue(1).ToString();

            string outputfilename = diffFolder + "\\" + diffObject + ".txt";
            string logfilename = Directory.GetCurrentDirectory() + "\\" + "csharplog.txt";

            try
            {
                ResetOutputFile(outputfilename);
                LoopFindVersionDifferences(ArcSdeWorkspaceFromFile(database), versionTarget, versionCompare, diffObject, outputfilename);
                //if (diffDirection == "1")
                //{
                //    //"1" means DATAQUALITY compared to Default
                //    LoopFindVersionDifferences(ArcSdeWorkspaceFromFile(database), "DATAQUALITY.DataQuality", "sde.Default", diffObject, outputfilename);
                //}
                //else if (diffDirection == "2")
                //{
                //    LoopFindVersionDifferences(ArcSdeWorkspaceFromFile(database), "sde.Default", "DATAQUALITY.DataQuality", diffObject, outputfilename);
                //}
            }
            catch (Exception e)
            {
                FileLogger.Instance.Open(logfilename, true);
                FileLogger.Instance.CreateEntry(e.Message);
                FileLogger.Instance.Close();
            }

            m_AOLicenseInitializer.ShutdownApplication();
        }


        public static void LoopFindVersionDifferences(IWorkspace workspace, String sourceVersionName, String targetVersionName, String tableName, string outputfilename)
        {
            Dictionary<string, IFIDSet> verDiffDict = new Dictionary<string, IFIDSet>();

            IFIDSet fidsetInsert = FindVersionDifferences(workspace, sourceVersionName, targetVersionName, tableName, esriDifferenceType.esriDifferenceTypeInsert);
            verDiffDict.Add("Insert", fidsetInsert);

            IFIDSet fidsetUpdateNoChange = FindVersionDifferences(workspace, sourceVersionName, targetVersionName, tableName, esriDifferenceType.esriDifferenceTypeUpdateNoChange);
            verDiffDict.Add("UpdateNoChange", fidsetUpdateNoChange);

            IFIDSet fidsetDeleteNoChange = FindVersionDifferences(workspace, sourceVersionName, targetVersionName, tableName, esriDifferenceType.esriDifferenceTypeDeleteNoChange);
            verDiffDict.Add("DeleteNoChange", fidsetDeleteNoChange);

            int abortFlag = 0;
            foreach (KeyValuePair<string, IFIDSet> pair in verDiffDict)
            {
                if (pair.Value.Count() > 0)
                {
                    abortFlag = 1;
                }
            }

            if (abortFlag == 1)
            {
                WriteDifferenceResults(verDiffDict, outputfilename);
            }
        }


        public static void ResetOutputFile(string outputfilename)
        {
            
            if (File.Exists(outputfilename))
            {
                File.Delete(outputfilename);
            }
        }


        public static void WriteDifferenceResults(Dictionary<string, IFIDSet> verDiffDict, string outputfilename)
        {
            int finalCounter = 0;
            System.Text.StringBuilder finalWriterString = new System.Text.StringBuilder();
            finalWriterString.Append("{");
            foreach (KeyValuePair<string, IFIDSet> pair in verDiffDict)
            {
                
                finalWriterString.Append('"' + pair.Key + '"' + " : " + '"' + "[");
                int iFID;
                for (int i = 0; i < pair.Value.Count(); i++)
                {
                    pair.Value.Next(out iFID);
                    //pair.Value is a IFIDSet which are the objectid's from the version compare results
                    if (i == pair.Value.Count() - 1)
                    {
                        finalWriterString.Append(iFID);
                    }
                    else
                    {
                        finalWriterString.Append(iFID + ",");
                    }
                }
                if (finalCounter == verDiffDict.Count - 1)
                {
                    finalWriterString.Append("]" + '"');
                }
                else
                {
                    finalWriterString.Append("]" + '"' + ", ");
                }
                finalCounter++;
            }

            finalWriterString.Append("}");

            try
            {
                using (StreamWriter file = new StreamWriter(outputfilename, true))
                {
                    file.WriteLine(finalWriterString);
                }
            }
            catch (Exception ex)
            {
                Environment.Exit(-1);
            }
        }


        public static IWorkspace ArcSdeWorkspaceFromFile(String connectionFile)
        {
            IWorkspaceFactory workspaceFactory = new SdeWorkspaceFactoryClass();
            return workspaceFactory.OpenFromFile(connectionFile, 0);
        }


        public static void GPListFeatureClasses(Geoprocessor GP)
        {
            // List all feature classes in the workspace starting with d.
            GP.SetEnvironmentValue("workspace", @"C:\Uc2003\Portland_OR.gdb");
            IGpEnumList fcs = GP.ListFeatureClasses("d*", "", "");
            string fc = fcs.Next();

            while (fc != "")
            {
                Console.WriteLine(fc);
                fc = fcs.Next();
            }
        }


        public static IFIDSet FindVersionDifferences(IWorkspace workspace, String sourceVersionName, String targetVersionName, String tableName, esriDifferenceType differenceType)
        {
            // Get references to the child and parent versions.
            IVersionedWorkspace versionedWorkspace = (IVersionedWorkspace)workspace;
            IVersion sourceVersion = versionedWorkspace.FindVersion(sourceVersionName);
            IVersion targetVersion = versionedWorkspace.FindVersion(targetVersionName);

            // Cast to the IVersion2 interface to find the common ancestor.
            IVersion2 sourceVersion2 = (IVersion2)sourceVersion;
            IVersion commonAncestorVersion = sourceVersion2.GetCommonAncestor(targetVersion);

            // Cast the child version to IFeatureWorkspace and open the table.
            IFeatureWorkspace targetFWS = (IFeatureWorkspace)sourceVersion;
            ITable targetTable = targetFWS.OpenTable(tableName);

            // Cast the common ancestor version to IFeatureWorkspace and open the table.
            IFeatureWorkspace commonAncestorFWS = (IFeatureWorkspace)commonAncestorVersion;
            ITable commonAncestorTable = commonAncestorFWS.OpenTable(tableName);

            // Cast to the IVersionedTable interface to create a difference cursor.
            IVersionedTable versionedTable = (IVersionedTable)targetTable;
            IDifferenceCursor differenceCursor = versionedTable.Differences(commonAncestorTable, differenceType, null);

            // Create output variables for the IDifferenceCursor.Next method and a FID set.
            IFIDSet fidSet = new FIDSetClass();
            IRow differenceRow = null;
            int objectID = -1;

            // Step through the cursor, showing the ID of each modified row.
            differenceCursor.Next(out objectID, out differenceRow);
            while (objectID != -1)
            {
                fidSet.Add(objectID);
                differenceCursor.Next(out objectID, out differenceRow);
            }

            fidSet.Reset();
            return fidSet;
        }
    }

}

