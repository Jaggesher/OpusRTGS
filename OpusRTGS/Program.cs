using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OpusRTGS
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RTGSRead rtgsRead = new RTGSRead();
                RTGSInbound rtgsInbound = new RTGSInbound();
                BBOutBoundData bbOutBoundData = new BBOutBoundData();
                RTGSStatusUpdate rtgsStatusUpdate = new RTGSStatusUpdate();

                while (true)
                {
                    Console.WriteLine("Executing...........");

                     rtgsRead.Run();
                     rtgsInbound.Run();
                     bbOutBoundData.Run();

                    //rtgsStatusUpdate.Run();

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
        private readonly string bat;
        private ProcessStartInfo psi;
        private Process process;
        public RTGSReturn()
        {
            try
            {
                //Test..
                bat = @"D:\OpusBatch\RTGS_Return.bat";

                //Deploy..
                //bat = @"D:\OpusBatch\RTGS_Return.bat";

                psi = new ProcessStartInfo();
                process = new Process();
                psi.CreateNoWindow = true; //This hides the dos-style black window that the command prompt usually shows
                psi.FileName = @"cmd.exe";
                psi.UseShellExecute = false;
                psi.Verb = "runas"; //This is what actually runs the command as administrator
                psi.Arguments = "/C " + bat;
                process.StartInfo = psi;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error For RTGS Read Constructor.");
                Console.WriteLine(e.Message);
            }
        }
        public void Run()
        {
            try
            {
                process.Start();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Shouldn't Open as administrator");
                Console.WriteLine(e.Message);
                //If you are here the user clicked decline to grant admin privileges (or he's not administrator)
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
        //private readonly 
    }
}
