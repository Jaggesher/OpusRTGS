using System;
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
                
                while (true)
                {
                    Console.WriteLine("Executing...........");

                    //rtgsRead.Run();
                    //rtgsInbound.Run();
                    //bbOutBoundData.Run();

                    rtgsReturn.Run();

                    //rtgsStatusUpdate.Run();

                    //stapStatusUpdate.Run();

                    Console.WriteLine(".....DONE......");
                    Console.WriteLine("-------------------------------------------------------------------------------\n");
                    Console.WriteLine("Waiting...........");

                    System.Threading.Thread.Sleep(1 * 10 * 1000);
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
            //Testing...
            SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\From";
            BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\Backup";
            DestinationFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\To";
            LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\XmlDataToREAD";
            ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";

            //Deploy...
            //SourceFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\From";
            //BackupFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\Backup";
            //DestinationFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\To";
            //LogFolder = @"E:\Jaggesher WorkSpace\RTGS\BackUpRTGSInWordLogFiles\SATP_IN";
            //ConnectionString = @"Data Source=DESKTOP-ALPFNNL;Initial Catalog=db_Goldfish;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";

        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                FileStream fs = new FileStream(LogFolder + "\\XML_READ" + timeStamp + ".txt", FileMode.CreateNew, FileAccess.Write);
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
                                        if (SplitFileName[1] == "TT")
                                        {

                                            //string Tmp = $"INSERT INTO RTGSInwordLog(Date, Remarks, XMLFileName) VALUES(getdate(), 'File Moved From BBOutBound To SATP Input', '{NormalFileName}')";
                                            //SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            //cmd.ExecuteScalar();
                                        }
                                        else
                                        {
                                            string Tmp1 = $"SELECT FileName FROM XMLDataUpload WHERE XMLFileName = '{NormalFileName}'";
                                            SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                            string mainFileName = (string)cmd1.ExecuteScalar();

                                            string Tmp = $"INSERT INTO RTGSInwordLog(Date, Remarks, XMLFileName, FileName) VALUES(getdate(), 'File Moved From XML To T24 READ', '{NormalFileName}','{mainFileName}')";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
                                        }

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
            //Testing...
            SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATP_IN\From";
            BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATP_IN\Backup";
            DestinationFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATP_IN\To";
            LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\SATP_IN";
            ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123;Pooling=true;Max Pool Size=32700;Integrated Security=True";

            //Deploy...
            //SourceFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\From";
            //BackupFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\Backup";
            //DestinationFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\To";
            //LogFolder = @"E:\Jaggesher WorkSpace\RTGS\BackUpRTGSInWordLogFiles\SATP_IN";
            //ConnectionString = @"Data Source=DESKTOP-ALPFNNL;Initial Catalog=db_Goldfish;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";

        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                FileStream fs = new FileStream(LogFolder + "\\SATP_IN" + timeStamp + ".txt", FileMode.CreateNew, FileAccess.Write);
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
            //Testing...
            SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\From";
            BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\Backup";
            DestinationFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\To";
            LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\BBOutBound_SATP";
            ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";

            //Deploy...
            //SourceFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\From";
            //BackupFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\Backup";
            //DestinationFolder = @"E:\Jaggesher WorkSpace\RTGS\SATP_IN\To";
            //LogFolder = @"E:\Jaggesher WorkSpace\RTGS\BackUpRTGSInWordLogFiles\SATP_IN";
            //ConnectionString = @"Data Source=DESKTOP-ALPFNNL;Initial Catalog=db_Goldfish;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";

        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                FileStream fs = new FileStream(LogFolder + "\\BBOut_IN" + timeStamp + ".txt", FileMode.CreateNew, FileAccess.Write);
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
            //Testing...
            //SourceFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\Return_READ\From";
            //BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\Return_READ\Backup";
            //DestinationFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\Return_READ\To";
            //LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\Return_IN";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";

            //Deploy...
            SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\ReturnInBound";
            BackupFolder = @"D:\RTGSFiles\ReturnInboundToInput";
            DestinationFolder = @"D:\distr_STPAdapter_v21_36\input";
            LogFolder = @"D:\RTGSFiles\LogFiles\ReturnInboundToInput";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";

        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                FileStream fs = new FileStream(LogFolder + "\\Return_IN" + timeStamp + ".txt", FileMode.CreateNew, FileAccess.Write);
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

                                        //string Tmp = $"INSERT INTO RTGSInwordLog(Date, Remarks, XMLFileName) VALUES(getdate(), 'File Moved From BBOutBound To SATP Input', '{NormalFileName}')";
                                        //SqlCommand cmd = new SqlCommand(Tmp, connection);
                                        //cmd.ExecuteScalar();

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
        private readonly string LogFile;
        private readonly string ConnectionString;
        private readonly string sourceFolder;

        public RTGSStatusUpdate()
        {
            //Deploy...

            //LogFile = @"D:\BackUpRTGSInWordLogFiles\RTGSStatusLog.txt";
            //ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            //sourceFolder = @"X:\AGR.WRITE";//Assuming Test is your Folder

            //Testing... 
            LogFile = @"D:\Opus\Development\Jogessor\RTGSStatusLog.txt";
            ConnectionString = @"Data Source=DESKTOP-ALPFNNL;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            sourceFolder = @"D:\Opus\Development\Jogessor\AGR.WRITE";//Assuming Test is your Folder

        }

        public void Run()
        {
            string flag = "success";

            try
            {
                // Connect to SQL
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    string FileName, TranNumber, Status, ErrMessage;
                    DirectoryInfo d = new DirectoryInfo(sourceFolder);
                    FileInfo[] Files = d.GetFiles("*.xml"); //Getting Text files

                    foreach (FileInfo file in Files)
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
                            // Console.WriteLine("I");
                        }
                        else if (SplitFileName[1] == "O" || SplitFileName[1] == "TT")
                        {
                            string Tmp = $"UPDATE dbo.RTGS SET TraNumber = '{TranNumber}', TrStatus = '{Status}', ErrDescription = '{ErrMessage}' WHERE XMLFileName = '{NormalFileName}'";
                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                            cmd.ExecuteScalar();
                            //Console.WriteLine("O");
                        }

                    }
                }
            }
            catch (SqlException e)
            {
                flag = e.ToString();
                //Console.WriteLine(e.ToString());
            }
            try
            {
                FileStream fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now + " | " + flag);
                Console.WriteLine(DateTime.Now + " | " + flag);
                sw.Flush();
                sw.Close();
                fs.Close();
            }
            catch (Exception e)
            {
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
            //Testing...
            SourceFolderErr = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATPStatus\Err";
            SourceFolderAck = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATPStatus\Ack";
            BackupFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\SATPStatus\Backup";
            LogFolder = @"D:\Opus\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\SATPStatus";
            ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";

            //Deploy...
            //SourceFolderErr = @"D:\distr_STPAdapter_v21_36\error";
            //SourceFolderAck = @"D:\distr_STPAdapter_v21_36\accepted";
            //BackupFolder = @"D:\RTGSFiles\SATPStatusUpdateBackup";
            //LogFolder = @"D:\RTGSFiles\LogFiles\SATPStatusUpdate";
            //ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";

            xmlDoc = new XmlDocument();

        }

        public void Run()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string timeStamp = dateTime.ToString("yyyyMMddHHmmssffff");
                FileStream fs = new FileStream(LogFolder + "\\SATP_Status" + timeStamp + ".txt", FileMode.CreateNew, FileAccess.Write);
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
                                        if (SplitFileName.Length > 1 && SplitFileName[1] == "BB")
                                        {
                                            string Tmp = $"UPDATE RTGS SET BBTraNumber = '{resultData}', BBErrDescription = 'N/A', BBTrStatus = '1' WHERE BBFileName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
                                        }
                                        else if (SplitFileName.Length > 1 && SplitFileName[1] == "Return")
                                        {
                                            string Tmp = $"UPDATE InwordReturn SET TrStatus = '1', TrNumber = '{resultData}', ErrDescription='N/A' WHERE RFIleName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
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

                                        if (SplitFileName.Length > 1 && SplitFileName[1] == "BB")
                                        {
                                            string Tmp = $"UPDATE RTGS SET BBTraNumber = 'N/A', BBErrDescription = '{ErrMessage}', BBTrStatus = '-1' WHERE BBFileName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();


                                        }
                                        else if (SplitFileName.Length > 1 && SplitFileName[1] == "Return")
                                        {
                                            string Tmp = $"UPDATE InwordReturn SET TrStatus = '-1', TrNumber = 'N/A', ErrDescription='{ErrMessage}' WHERE RFIleName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
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
}
