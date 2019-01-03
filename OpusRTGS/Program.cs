using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace OpusRTGS
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RTGSRead rtgsRead = new RTGSRead();
                RTGSReturn rtgsReturn = new RTGSReturn();
                RTGSInbound rtgsInbound = new RTGSInbound();
                BBOutBoundData bbOutBoundData = new BBOutBoundData();
                RTGSStatusUpdate rtgsStatusUpdate = new RTGSStatusUpdate();
                SATPStatusUpdate stapStatusUpdate = new SATPStatusUpdate();
                InboundFileProcess inboundFileProcess = new InboundFileProcess();

                while (true)
                {
                    Console.WriteLine("Executing...........");

                    #region Operations

                    //rtgsRead.Run();
                    //rtgsInbound.Run();
                    //bbOutBoundData.Run();

                    //rtgsReturn.Run();//In Production.

                    //rtgsStatusUpdate.Run();

                    //stapStatusUpdate.Run();//In Production

                    inboundFileProcess.Run();
                    #endregion

                    Console.WriteLine(".....DONE......");
                    Console.WriteLine("-------------------------------------------------------------------------------\n");
                    Console.WriteLine("Waiting...........");

                    System.Threading.Thread.Sleep(1 * 60 * 1000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Interrapt From Main Thread");
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }

    public class RTGSRead
    {
        private readonly string SourceFolder;
        private readonly string BackupFolder;
        private readonly string DestinationFolder;
        private readonly string LogFolder;
        private readonly string ConnectionString;

        public RTGSRead()
        {
            #region Testing...
            //SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\From";
            //BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\Backup";
            //DestinationFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\To";
            //LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\XmlDataToREAD";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion


            #region Deploy...
            SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\xmldata";
            BackupFolder = @"D:\RTGSFiles\xmlToREAD";
            DestinationFolder = @"X:\AGR.READ";
            LogFolder = @"D:\RTGSFiles\LogFiles\xmlToRead";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion

        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                string LogFileName = LogFolder + "\\XML_READ" + timeStamp + ".txt";
                FileStream fs = new FileStream(LogFileName, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(dateTime);
                int AffectedFileCount = 0;
                if (Directory.Exists(SourceFolder) && Directory.Exists(DestinationFolder) && Directory.Exists(BackupFolder))
                {
                    DirectoryInfo info = new DirectoryInfo(SourceFolder);
                    FileInfo[] files = info.GetFiles("*.xml").ToArray();

                    if (files.Count() != 0)
                    {
                        using (SqlConnection connection = new SqlConnection(ConnectionString))
                        {
                            connection.Open();
                            foreach (FileInfo file in files)
                            {
                                if (File.Exists(file.FullName))
                                {
                                    sw.Write(file.FullName);
                                    try
                                    {
                                        File.Copy(file.FullName, DestinationFolder + "\\" + file.Name, true);
                                        sw.Write(" | Coppied  successfully | ");
                                        if (File.Exists(BackupFolder + "\\" + file.Name))
                                        {
                                            File.Delete(BackupFolder + "\\" + file.Name);
                                            sw.Write("Duplicate Name | ");
                                        }

                                        File.Move(file.FullName, BackupFolder + "\\" + file.Name);
                                        sw.Write("Moved successfully");
                                        sw.WriteLine();

                                        string NormalFileName = Path.GetFileNameWithoutExtension(file.Name);
                                        string[] SplitFileName = NormalFileName.Split('_');
                                        string mainFileName = "Can't Find";

                                        if (SplitFileName[1] == "TT")
                                        {
                                            string Tmp1 = $"SELECT BBFileName FROM RTGS WHERE XMLFileName = '{file.Name}'";
                                            SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                            mainFileName = (string)cmd1.ExecuteScalar();
                                            // Console.WriteLine("TT Entry :) ");
                                        }
                                        else
                                        {
                                            string Tmp1 = $"SELECT FileName FROM XMLDataUpload WHERE XMLFileName = '{NormalFileName}'";
                                            SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                            mainFileName = (string)cmd1.ExecuteScalar();

                                        }

                                        string Tmp = $"INSERT INTO RTGSInwordLog(Date, Remarks, XMLFileName, FileName) VALUES(getdate(), 'File Moved From XML To T24 READ', '{NormalFileName}','{mainFileName}')";
                                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                                        cmd.ExecuteScalar();

                                        AffectedFileCount++;

                                    }
                                    catch (IOException e)
                                    {
                                        sw.WriteLine(e.Message);
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                else
                                {
                                    sw.WriteLine("File Cn't Found");
                                }
                            }
                        }
                    }
                    else
                    {
                        sw.WriteLine("Empty | No Files For Coppy");
                    }
                }
                else
                {
                    sw.WriteLine("Can't find the folders. Please Communicat with Opus Team.");
                }

                Console.WriteLine(AffectedFileCount.ToString() + ", Files Affected For XML To T24Read");

                sw.WriteLine(AffectedFileCount.ToString() + ", Files Affected");
                sw.Flush();
                sw.Close();
                fs.Close();

                if (File.Exists(LogFileName) && AffectedFileCount == 0)
                {
                    File.Delete(LogFileName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't Create Log File Or Database Connection fail, Operation Ignored");
                Console.WriteLine(e.Message);
                //If you are here the user clicked decline to grant admin privileges (or he's not administrator)
            }
        }
    }

    public class RTGSInbound
    {
        private readonly string SourceFolder;
        private readonly string BackupFolder;
        private readonly string DestinationFolder;
        private readonly string LogFolder;
        private readonly string ConnectionString;

        public RTGSInbound()
        {
            #region Testing...
            //SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATP_IN\From";
            //BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATP_IN\Backup";
            //DestinationFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATP_IN\To";
            //LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\SATP_IN";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion

            #region Deploy...
            SourceFolder = @"D:\distr_STPAdapter_v21_36\output";
            BackupFolder = @"D:\RTGSFiles\SATPToInbound";
            DestinationFolder = @"C:\inetpub\wwwroot\RTGS\Upload\InBoundData";
            LogFolder = @"D:\RTGSFiles\LogFiles\SATPToInbound";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion
        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                string LogFileName = LogFolder + "\\SATP_IN" + timeStamp + ".txt";
                FileStream fs = new FileStream(LogFileName, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(dateTime);
                int AffectedFileCount = 0;
                if (Directory.Exists(SourceFolder) && Directory.Exists(DestinationFolder) && Directory.Exists(BackupFolder))
                {
                    DirectoryInfo info = new DirectoryInfo(SourceFolder);
                    FileInfo[] files = info.GetFiles("*.xml").ToArray();

                    if (files.Count() != 0)
                    {
                        using (SqlConnection connection = new SqlConnection(ConnectionString))
                        {
                            connection.Open();
                            foreach (FileInfo file in files)
                            {
                                if (File.Exists(file.FullName))
                                {
                                    sw.Write(file.FullName);
                                    try
                                    {
                                        File.Copy(file.FullName, DestinationFolder + "\\" + file.Name, true);
                                        sw.Write(" | Coppied  successfully | ");
                                        if (File.Exists(BackupFolder + "\\" + file.Name))
                                        {
                                            File.Delete(BackupFolder + "\\" + file.Name);
                                            sw.Write("Duplicate Name | ");
                                        }

                                        File.Move(file.FullName, BackupFolder + "\\" + file.Name);
                                        sw.Write("Moved successfully");
                                        sw.WriteLine();

                                        string NormalFileName = Path.GetFileNameWithoutExtension(file.Name);

                                        string Tmp = $"INSERT INTO RTGSInwordLog(Date, Remarks, FileName) VALUES(getdate(), 'File Moved From SATP To Upload_InboundData', '{NormalFileName}')";
                                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                                        cmd.ExecuteScalar();

                                        AffectedFileCount++;

                                    }
                                    catch (IOException e)
                                    {
                                        sw.WriteLine(e.Message);
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                else
                                {
                                    sw.WriteLine("File Cn't Found");
                                }
                            }
                        }
                    }
                    else
                    {
                        sw.WriteLine("Empty | No Files For Coppy");
                    }
                }
                else
                {
                    sw.WriteLine("Can't find the folders. Please Communicat with Opus Team.");
                }

                Console.WriteLine(AffectedFileCount.ToString() + ", Files Affected For SATP To Inbound");

                sw.WriteLine(AffectedFileCount.ToString() + ", Files Affected");
                sw.Flush();
                sw.Close();
                fs.Close();

                if (File.Exists(LogFileName) && AffectedFileCount == 0)
                {
                    File.Delete(LogFileName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't Create Log File Or Database Connection fail, Operation Ignored");
                Console.WriteLine(e.Message);
                //If you are here the user clicked decline to grant admin privileges (or he's not administrator)
            }
        }
    }

    public class BBOutBoundData
    {
        private readonly string SourceFolder;
        private readonly string BackupFolder;
        private readonly string DestinationFolder;
        private readonly string LogFolder;
        private readonly string ConnectionString;

        public BBOutBoundData()
        {
            #region Testing...
            //SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\From";
            //BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\Backup";
            //DestinationFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\To";
            //LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\BBOutBound_SATP";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion

            #region Deploy...
            SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\BBOutBoundData";
            BackupFolder = @"D:\RTGSFiles\BBOutToSATP";
            DestinationFolder = @"D:\distr_STPAdapter_v21_36\input";
            LogFolder = @"D:\RTGSFiles\LogFiles\BBOutToSATP";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion
        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                string LogFileName = LogFolder + "\\BBOut_IN" + timeStamp + ".txt";
                FileStream fs = new FileStream(LogFileName, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(dateTime);
                int AffectedFileCount = 0;
                if (Directory.Exists(SourceFolder) && Directory.Exists(DestinationFolder) && Directory.Exists(BackupFolder))
                {
                    DirectoryInfo info = new DirectoryInfo(SourceFolder);
                    FileInfo[] files = info.GetFiles("*.xml").ToArray();

                    if (files.Count() != 0)
                    {
                        using (SqlConnection connection = new SqlConnection(ConnectionString))
                        {
                            connection.Open();
                            foreach (FileInfo file in files)
                            {
                                if (File.Exists(file.FullName))
                                {
                                    sw.Write(file.FullName);
                                    try
                                    {
                                        File.Copy(file.FullName, DestinationFolder + "\\" + file.Name, true);
                                        sw.Write(" | Coppied  successfully | ");
                                        if (File.Exists(BackupFolder + "\\" + file.Name))
                                        {
                                            File.Delete(BackupFolder + "\\" + file.Name);
                                            sw.Write("Duplicate Name | ");
                                        }

                                        File.Move(file.FullName, BackupFolder + "\\" + file.Name);
                                        sw.Write("Moved successfully");
                                        sw.WriteLine();

                                        string NormalFileName = Path.GetFileNameWithoutExtension(file.Name);

                                        string Tmp = $"INSERT INTO RTGSInwordLog(Date, Remarks, XMLFileName) VALUES(getdate(), 'File Moved From BBOutBound To SATP Input', '{NormalFileName}')";
                                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                                        cmd.ExecuteScalar();

                                        AffectedFileCount++;

                                    }
                                    catch (IOException e)
                                    {
                                        sw.WriteLine(e.Message);
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                else
                                {
                                    sw.WriteLine("File Cn't Found");
                                }
                            }
                        }
                    }
                    else
                    {
                        sw.WriteLine("Empty | No Files For Coppy");
                    }
                }
                else
                {
                    sw.WriteLine("Can't find the folders. Please Communicat with Opus Team.");
                }

                Console.WriteLine(AffectedFileCount.ToString() + ", Files Affected For BBOut To Input");

                sw.WriteLine(AffectedFileCount.ToString() + ", Files Affected");
                sw.Flush();
                sw.Close();
                fs.Close();
                if (File.Exists(LogFileName) && AffectedFileCount == 0)
                {
                    File.Delete(LogFileName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't Create Log File Or Database Connection fail, Operation Ignored");
                Console.WriteLine(e.Message);
            }
        }
    }

    public class RTGSReturn
    {
        private readonly string SourceFolder;
        private readonly string BackupFolder;
        private readonly string DestinationFolder;
        private readonly string LogFolder;
        private readonly string ConnectionString;

        public RTGSReturn()
        {
            #region  Testing...
            //SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\Return_READ\From";
            //BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\Return_READ\Backup";
            //DestinationFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\Return_READ\To";
            //LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\Return_IN";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion

            #region Deploy...
            SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\ReturnInBound";
            BackupFolder = @"D:\RTGSFiles\ReturnInboundToInput";
            DestinationFolder = @"D:\distr_STPAdapter_v21_36\input";
            LogFolder = @"D:\RTGSFiles\LogFiles\ReturnInboundToInput";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion
        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                string LogFileName = LogFolder + "\\Return_IN" + timeStamp + ".txt";
                FileStream fs = new FileStream(LogFileName, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(dateTime);
                int AffectedFileCount = 0;
                if (Directory.Exists(SourceFolder) && Directory.Exists(DestinationFolder) && Directory.Exists(BackupFolder))
                {
                    DirectoryInfo info = new DirectoryInfo(SourceFolder);
                    FileInfo[] files = info.GetFiles("*.xml").ToArray();

                    if (files.Count() != 0)
                    {
                        using (SqlConnection connection = new SqlConnection(ConnectionString))
                        {
                            connection.Open();
                            foreach (FileInfo file in files)
                            {
                                if (File.Exists(file.FullName))
                                {
                                    sw.Write(file.FullName);
                                    try
                                    {
                                        File.Copy(file.FullName, DestinationFolder + "\\" + file.Name, true);
                                        sw.Write(" | Coppied  successfully | ");
                                        if (File.Exists(BackupFolder + "\\" + file.Name))
                                        {
                                            File.Delete(BackupFolder + "\\" + file.Name);
                                            sw.Write("Duplicate Name | ");
                                        }

                                        File.Move(file.FullName, BackupFolder + "\\" + file.Name);
                                        sw.Write("Moved successfully");
                                        sw.WriteLine();

                                        string NormalFileName = Path.GetFileNameWithoutExtension(file.Name);

                                        string Tmp = $"INSERT INTO RTGSInwordLog(Date, Remarks, XMLFileName) VALUES(getdate(), 'File Moved From ReturnInBound To SATP Input', '{NormalFileName}')";
                                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                                        cmd.ExecuteScalar();

                                        AffectedFileCount++;

                                    }
                                    catch (IOException e)
                                    {
                                        sw.WriteLine(e.Message);
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                else
                                {
                                    sw.WriteLine("File Cn't Found");
                                }
                            }
                        }
                    }
                    else
                    {
                        sw.WriteLine("Empty | No Files For Coppy");
                    }
                }
                else
                {
                    sw.WriteLine("Can't find the folders. Please Communicat with Opus Team.");
                }

                Console.WriteLine(AffectedFileCount.ToString() + ", Files Affected For Return To Input");

                sw.WriteLine(AffectedFileCount.ToString() + ", Files Affected");
                sw.Flush();
                sw.Close();
                fs.Close();

                if (File.Exists(LogFileName) && AffectedFileCount == 0)
                {
                    File.Delete(LogFileName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't Create Log File Or Database Connection fail, Operation Ignored");
                Console.WriteLine(e.Message);
            }
        }
    }

    public class RTGSStatusUpdate
    {
        private readonly string LogFolder;
        private readonly string BackupFolder;
        private readonly string ConnectionString;
        private readonly string SourceFolder;

        public RTGSStatusUpdate()
        {

            #region Testing...  
            //LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\RTGSStatus";
            //ConnectionString = @"Data Source=DESKTOP-ALPFNNL;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            //SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\RTGSStatus\Source";//Assuming Test is your Folder
            //BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\RTGSStatus\backup";
            #endregion

            #region Deploy...
            LogFolder = @"D:\RTGSFiles\LogFiles\T24StatusUpdate";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            SourceFolder = @"X:\AGR.WRITE";//Assuming Test is your Folder
            BackupFolder = @"D:\RTGSFiles\T24WriteBackup";
            #endregion


        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                string LogFileName = LogFolder + "\\T24Status" + timeStamp + ".txt";
                FileStream fs = new FileStream(LogFileName, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(dateTime);
                int AffectedFileCount = 0;
                if (Directory.Exists(SourceFolder) && Directory.Exists(BackupFolder))
                {
                    DirectoryInfo info = new DirectoryInfo(SourceFolder);
                    FileInfo[] files = info.GetFiles().ToArray();

                    if (files.Count() != 0)
                    {
                        using (SqlConnection connection = new SqlConnection(ConnectionString))
                        {
                            connection.Open();
                            string FileName, TranNumber, Status, ErrMessage;
                            foreach (FileInfo file in files)
                            {
                                if (File.Exists(file.FullName))
                                {
                                    try
                                    {
                                        if (!File.Exists(BackupFolder + "\\" + file.Name))
                                        {
                                            FileName = file.Name;
                                            TranNumber = "N/A";
                                            Status = "N/A";
                                            ErrMessage = "N/A";

                                            string content = File.ReadAllText(file.FullName);

                                            string[] contents = content.Split(',');

                                            if (contents.Length > 0)
                                            {
                                                //First TRAN NUmber
                                                TranNumber = contents[0];
                                                string[] FirstFields = contents[0].Split('/');

                                                //Status Code
                                                Status = FirstFields[2];
                                                if (FirstFields[2] != "1")
                                                {
                                                    //Error Message
                                                    ErrMessage = contents[1];
                                                }
                                            }

                                            string NormalFileName = Path.GetFileNameWithoutExtension(FileName);
                                            string[] SplitFileName = NormalFileName.Split('_');

                                            if (SplitFileName[1] == "I")
                                            {
                                                string Tmp = $"UPDATE dbo.XMLDataUpload SET TraNumber = '{TranNumber}', TrStatus = '{Status}', ErrDescription = '{ErrMessage}' WHERE XMLFileName = '{NormalFileName}'";
                                                SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                cmd.ExecuteScalar();

                                                SqlCommand cmdTm = new SqlCommand("spLogInward", connection);
                                                cmdTm.CommandType = CommandType.StoredProcedure;
                                                cmdTm.Parameters.AddWithValue("@fileName", NormalFileName);
                                                if (Status == "1")
                                                {
                                                    cmdTm.Parameters.AddWithValue("@Remarks ", "Success");
                                                }
                                                else
                                                {
                                                    cmdTm.Parameters.AddWithValue("@Remarks ", "Transiction Fail");
                                                }
                                                cmdTm.ExecuteScalar();

                                                // Console.WriteLine("I");
                                            }
                                            else if (SplitFileName[1] == "TT")
                                            {
                                                string Tmp = $"UPDATE dbo.RTGS SET TraNumber = '{TranNumber}', TrStatus = '{Status}', ErrDescription = '{ErrMessage}' WHERE XMLFileName = '{FileName}'";
                                                SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                cmd.ExecuteScalar();

                                                SqlCommand cmdTm = new SqlCommand("SP_Post_Rtgs_StatusLog_By_Batch", connection);
                                                cmdTm.CommandType = CommandType.StoredProcedure;
                                                cmdTm.Parameters.AddWithValue("@FileName", file.Name);
                                                cmdTm.Parameters.AddWithValue("@StatusID", Status);
                                                cmdTm.Parameters.AddWithValue("@ProcessType", "TT");
                                                cmdTm.ExecuteScalar();

                                                //Console.WriteLine("TT");
                                            }

                                            sw.Write(file.FullName);
                                            File.Copy(file.FullName, BackupFolder + "\\" + file.Name, true);
                                            sw.Write(" | Updated successfully");
                                            sw.WriteLine();

                                            AffectedFileCount++;
                                        }

                                    }
                                    catch (IOException e)
                                    {
                                        sw.WriteLine(e.Message);
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                else
                                {
                                    sw.WriteLine("File Cn't Found");
                                }
                            }
                        }
                    }
                    else
                    {
                        sw.WriteLine("Empty | No Files For Coppy");
                    }
                }
                else
                {
                    sw.WriteLine("Can't find the folders. Please Communicat with Opus Team.");
                }

                Console.WriteLine(AffectedFileCount.ToString() + ", Files Affected For T24 Status Update");

                sw.WriteLine(AffectedFileCount.ToString() + ", Files Affected");
                sw.Flush();
                sw.Close();
                fs.Close();

                if (File.Exists(LogFileName) && AffectedFileCount == 0)
                {
                    File.Delete(LogFileName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't Create Log File Or Database Connection fail, Operation Ignored");
                Console.WriteLine(e.Message);
            }
        }
    }

    public class SATPStatusUpdate
    {
        private readonly string SourceFolderErr;
        private readonly string SourceFolderAck;
        private readonly string BackupFolder;
        private readonly string LogFolder;
        private readonly string ConnectionString;
        private readonly XmlDocument xmlDoc;

        public SATPStatusUpdate()
        {
            #region Testing...
            //SourceFolderErr = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATPStatus\Err";
            //SourceFolderAck = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATPStatus\Ack";
            //BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATPStatus\Backup";
            //LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\SATPStatus";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion

            #region Deploy...
            SourceFolderErr = @"D:\distr_STPAdapter_v21_36\error";
            SourceFolderAck = @"D:\distr_STPAdapter_v21_36\accepted";
            BackupFolder = @"D:\RTGSFiles\SATPStatusUpdateBackup";
            LogFolder = @"D:\RTGSFiles\LogFiles\SATPStatusUpdate";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion

            xmlDoc = new XmlDocument();

        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                string LogFileName = LogFolder + "\\SATP_Status" + timeStamp + ".txt";
                FileStream fs = new FileStream(LogFileName, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(dateTime);
                int AffectedFileCount = 0;
                if (Directory.Exists(SourceFolderAck) && Directory.Exists(SourceFolderErr) && Directory.Exists(BackupFolder))
                {
                    using (SqlConnection connection = new SqlConnection(ConnectionString))
                    {
                        connection.Open();

                        DirectoryInfo infoAck = new DirectoryInfo(SourceFolderAck);
                        FileInfo[] Ackfiles = infoAck.GetFiles().ToArray();

                        if (Ackfiles.Count() > 0)
                        {
                            foreach (FileInfo file in Ackfiles)
                            {
                                if (!File.Exists(BackupFolder + "//" + file.Name))
                                {
                                    try
                                    {
                                        File.Copy(file.FullName, BackupFolder + "\\" + file.Name, true);
                                        sw.WriteLine(file.FullName);

                                        xmlDoc.Load(file.FullName);
                                        string resultData = "N/A";

                                        XmlNodeList elemListMIR = xmlDoc.GetElementsByTagName("MIR");
                                        if (elemListMIR.Count > 0) resultData = elemListMIR[0].InnerText;
                                        resultData += ",";
                                        XmlNodeList elemListSignature = xmlDoc.GetElementsByTagName("Signature");
                                        if (elemListSignature.Count > 0) resultData += elemListSignature[0].InnerText;
                                        else resultData += "N/A";

                                        string NormalizeFileName = Path.GetFileNameWithoutExtension(file.Name);
                                        string[] SplitFileName = NormalizeFileName.Split('_');

                                        SqlCommand cmdTm = new SqlCommand("SP_Post_Rtgs_StatusLog_By_Batch", connection);
                                        cmdTm.CommandType = CommandType.StoredProcedure;
                                        cmdTm.Parameters.AddWithValue("@FileName", NormalizeFileName);
                                        cmdTm.Parameters.AddWithValue("@StatusID", 1);
                                        cmdTm.Parameters.AddWithValue("@ProcessType", "BB");

                                        if (SplitFileName.Length > 1 && SplitFileName[1] == "BB")
                                        {
                                            string Tmp = $"UPDATE RTGS SET BBTraNumber = '{resultData}', BBErrDescription = 'N/A', BBTrStatus = '1' WHERE BBFileName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
                                            cmdTm.ExecuteScalar();
                                        }
                                        else if (SplitFileName.Length > 1 && SplitFileName[1] == "Return")
                                        {
                                            string Tmp = $"UPDATE InwordReturn SET TrStatus = '1', TrNumber = '{resultData}', ErrDescription='N/A' WHERE RFIleName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
                                            cmdTm.ExecuteScalar();
                                        }

                                        //Aditional Logic

                                        AffectedFileCount++;
                                    }
                                    catch (IOException e)
                                    {
                                        sw.WriteLine(e.Message);
                                        Console.WriteLine(e.Message);
                                    }
                                }
                            }
                        }
                        else
                        {
                            sw.WriteLine("Empty | No files in SATP acknowledge folder");
                        }



                        DirectoryInfo infoErr = new DirectoryInfo(SourceFolderErr);
                        FileInfo[] Errfiles = infoErr.GetFiles().ToArray();

                        if (Errfiles.Count() > 0)
                        {
                            foreach (FileInfo file in Errfiles)
                            {
                                if (!File.Exists(BackupFolder + "//" + file.Name))
                                {
                                    try
                                    {
                                        File.Copy(file.FullName, BackupFolder + "\\" + file.Name, true);
                                        sw.WriteLine(file.FullName);
                                        char[] CharsToTrim = { '\'', '"' };
                                        string ErrMessage = File.ReadLines(file.FullName).First();
                                        ErrMessage = ErrMessage.Replace("'", " ");

                                        string NormalizeFileName = Path.GetFileNameWithoutExtension(file.Name);
                                        string[] SplitFileName = NormalizeFileName.Split('_');

                                        SqlCommand cmdTm = new SqlCommand("SP_Post_Rtgs_StatusLog_By_Batch", connection);
                                        cmdTm.CommandType = CommandType.StoredProcedure;
                                        cmdTm.Parameters.AddWithValue("@FileName", NormalizeFileName);
                                        cmdTm.Parameters.AddWithValue("@StatusID", 1);
                                        cmdTm.Parameters.AddWithValue("@ProcessType", "BB");

                                        if (SplitFileName.Length > 1 && SplitFileName[1] == "BB")
                                        {
                                            string Tmp = $"UPDATE RTGS SET BBTraNumber = 'N/A', BBErrDescription = '{ErrMessage}', BBTrStatus = '-1' WHERE BBFileName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
                                            cmdTm.ExecuteScalar();
                                        }
                                        else if (SplitFileName.Length > 1 && SplitFileName[1] == "Return")
                                        {
                                            string Tmp = $"UPDATE InwordReturn SET TrStatus = '-1', TrNumber = 'N/A', ErrDescription='{ErrMessage}' WHERE RFIleName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
                                            cmdTm.ExecuteScalar();
                                        }

                                        //Aditional Logic

                                        AffectedFileCount++;
                                    }
                                    catch (IOException e)
                                    {
                                        sw.WriteLine(e.Message);
                                        Console.WriteLine(e.Message);
                                    }
                                }
                            }
                        }
                        else
                        {
                            sw.WriteLine("Empty | No files in SATP err folder");
                        }

                    }
                }
                else
                {
                    sw.WriteLine("Can't find the folders. Please Communicat with Opus Team.");
                }

                Console.WriteLine(AffectedFileCount.ToString() + ", Files Affected For SATP Status Update");

                sw.WriteLine(AffectedFileCount.ToString() + ", Files Affected");
                sw.Flush();
                sw.Close();
                fs.Close();

                if (File.Exists(LogFileName) && AffectedFileCount == 0)
                {
                    File.Delete(LogFileName);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Can't Create Log File Or Database Connection fail, SATP Status Update Operation Ignored");
                Console.WriteLine(e.Message);
            }
        }
    }

    public class SealXmlFile
    {
        private readonly XmlDocument xmlDoc;
        private static SealXmlFile instance = new SealXmlFile();

        private SealXmlFile()
        {
            xmlDoc = new XmlDocument();
        }

        public string SealXml(string FileFullPath)
        {
            try
            {
                xmlDoc.Load(FileFullPath);
                XmlElement record = xmlDoc.CreateElement("ABL_Opus");
                record.SetAttribute("type", "General Status.");
                record.InnerText = "Posted-" + DateTime.Now;
                xmlDoc.DocumentElement.AppendChild(record);
                xmlDoc.Save(FileFullPath);
                return "Success";
            }
            catch (XmlException e)
            {
                Console.WriteLine("Error from xml seal Operation.");
                Console.WriteLine(e.Message);
                return "Fail";
            }
        }

        public static SealXmlFile getInstance()
        {
            return instance;
        }
    }

    public class InboundFileProcess
    {
        private readonly string SourceFolder;
        private readonly string LogFolder;
        private readonly string ConnectionString;

        public InboundFileProcess()
        {
            #region Testing...
            SourceFolder = @"D:\Opus\Development\Jogessor\newfile\InBoundData";
            LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\InboundFileProcessLog";
            ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion


            #region Deploy...
            //SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\xmldata";
            //LogFolder = @"D:\RTGSFiles\LogFiles\xmlToRead";
            //ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion

        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                string LogFileName = LogFolder + "\\InboundFileProcessLog" + timeStamp + ".txt";
                FileStream fs = new FileStream(LogFileName, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(dateTime);
                int AffectedFileCount = 0;
                if (Directory.Exists(SourceFolder))
                {
                    DirectoryInfo info = new DirectoryInfo(SourceFolder);
                    FileInfo[] files = info.GetFiles("*.xml").ToArray();
                    string FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId;
                    XmlDocument doc = new XmlDocument();

                    if (files.Count() != 0)
                    {
                        using (SqlConnection connection = new SqlConnection(ConnectionString))
                        {
                            connection.Open();
                            foreach (FileInfo file in files)
                            {
                                FileName = MsgDefIdr = BizMsgIdr = CreDt = DebDt = Amt = AcctId = NtryRef = InstrId = AnyBIC = OrgnlInstrId = "N/A";

                                if (File.Exists(file.FullName))
                                {
                                    sw.Write(file.FullName);
                                    try
                                    {
                                        string Tmp1 = $"Select TOP 1 FileName From InboundDataBatch WHERE FileName = '{file.Name}';";
                                        SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                        string TableFileName = (string)cmd1.ExecuteScalar();  
                                        if (TableFileName != null) continue;
                                        doc.Load(file.FullName);

                                        FileName = file.Name;

                                        XmlNodeList elemList = doc.GetElementsByTagName("MsgDefIdr");
                                        if (elemList.Count > 0)
                                        {
                                            MsgDefIdr = elemList[0].InnerText;
                                            string[] FileTypePart = MsgDefIdr.Split('.');

                                            if (FileTypePart.Count() > 1)
                                            {
                                                string TempName = FileTypePart[0] + FileTypePart[1];
                                                if (TempName == "camt054" || TempName == "camt053" || TempName == "camt052")//camt.054
                                                {
                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");
                                                    AcctId = InerTextOfTag(doc, "Acct");
                                                    NtryRef = InerTextOfTag(doc, "NtryRef");
                                                    InstrId = InerTextOfTag(doc, "InstrId");
                                                    AnyBIC = InerTextOfTag(doc, "AnyBIC");
                                                    Amt = InerTextOfTag(doc, "Amt");
                                                }
                                                else if (TempName == "pacs002")//pacs.002
                                                {
                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");
                                                    OrgnlInstrId = InerTextOfTag(doc, "OrgnlInstrId");
                                                    Amt = InerTextOfTag(doc, "IntrBkSttlmAmt");
                                                }
                                                else
                                                {
                                                    //Console.WriteLine(MsgDefIdr);
                                                    continue;
                                                }

                                                sw.WriteLine(file.FullName);

                                                string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, DateTime)  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}', getdate());";
                                                SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                cmd.ExecuteScalar();

                                                AffectedFileCount++;

                                            }


                                        }

                                    }
                                    catch (IOException e)
                                    {
                                        sw.WriteLine(e.Message);
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                else
                                {
                                    sw.WriteLine("File Cn't Found");
                                }
                            }
                        }
                    }
                    else
                    {
                        sw.WriteLine("Empty | No  Files For Update");
                    }
                }
                else
                {
                    sw.WriteLine("Can't find the folders. Please Communicat with Opus Team.");
                }

                Console.WriteLine(AffectedFileCount.ToString() + ", Files Affected For Inbound File Process");

                sw.WriteLine(AffectedFileCount.ToString() + ", Files Affected");
                sw.Flush();
                sw.Close();
                fs.Close();

                if (File.Exists(LogFileName) && AffectedFileCount == 0)
                {
                    File.Delete(LogFileName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't Create Log File Or Database Connection fail, Operation Ignored");
                Console.WriteLine(e.Message);
                //If you are here the user clicked decline to grant admin privileges (or he's not administrator)
            }
        }

        private string InerTextOfTag(XmlDocument doc, string tagName)
        {
            XmlNodeList elemList = doc.GetElementsByTagName(tagName);
            if (elemList.Count > 0)
            {
                if (tagName == "Acct")
                {
                    for (int i = 0; i < elemList[0].ChildNodes.Count; i++)
                        if (elemList[0].ChildNodes[i].Name == "Id") return elemList[0].ChildNodes[i].InnerText;
                }
                else
                    return elemList[0].InnerText;
            }
            return "N/A";
        }

    }
}
