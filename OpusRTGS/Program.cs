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
                    rtgsStatusUpdate.Run();
                    rtgsRead.Run();
                    rtgsInbound.Run();
                    bbOutBoundData.Run();
                    rtgsReturn.Run();
                    stapStatusUpdate.Run();
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
        private readonly string RejectedFolder;
        private readonly string RawBackupFolder;
        private readonly XmlDocument doc;
        private readonly string ConnectionString;
        private readonly HandleDuplicate handleDuplicate;
        private readonly SealXmlFile sealXmlFile;


        public RTGSRead()
        {
            #region Testing...
            //SourceFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\From";
            //BackupFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\Backup";
            //DestinationFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\XmlDataToREAD\To";
            //LogFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\XmlDataToREAD";
            //RejectedFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BatchReject";
            //RawBackupFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\RAWXmlToREAD";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion


            #region Deploy...
            SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\xmldata";
            BackupFolder = @"D:\RTGSFiles\xmlToREAD";
            DestinationFolder = @"X:\AGR.READ";
            LogFolder = @"D:\RTGSFiles\LogFiles\xmlToRead";
            RejectedFolder = @"D:\RTGSFiles\BatchReject";
            RawBackupFolder = @"D:\RTGSFiles\RAWXmlToREAD";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion

            handleDuplicate = HandleDuplicate.getInstance();
            sealXmlFile = SealXmlFile.getInstance();
            doc = new XmlDocument();
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
                int DuplicateFileCount = 0;
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

                                    if (!handleDuplicate.IsExixtsFile(connection, file.Name, "xmlToRead"))
                                    {
                                        try
                                        {
                                            string NormalFileName = Path.GetFileNameWithoutExtension(file.Name);
                                            string[] SplitFileName = NormalFileName.Split('_');

                                            bool flag = false;

                                            if (SplitFileName[1] == "I")
                                            {
                                                flag = false;

                                                File.Copy(file.FullName, RawBackupFolder + "//" + file.Name, true);

                                                sealXmlFile.SealXml(RawBackupFolder + "//" + file.Name);

                                                doc.Load(file.FullName);
                                                string AccountNumber, Amount, InstrId, xmlSorceFileName;

                                                AccountNumber = InstrId = xmlSorceFileName = "N/A";
                                                Amount = "0";

                                                XmlNodeList elements = doc.GetElementsByTagName("CREDIT_ACCT_NO");
                                                if (elements.Count > 0)
                                                {
                                                    AccountNumber = elements[0].InnerText;
                                                }


                                                elements = doc.GetElementsByTagName("DEBIT_AMOUNT");
                                                if (elements.Count > 0)
                                                {
                                                    Amount = elements[0].InnerText;
                                                    string[] ignoreDot = Amount.Split('.');
                                                    if (ignoreDot.Count() > 0)
                                                        Amount = ignoreDot[0];
                                                }


                                                elements = doc.GetElementsByTagName("FILE_NAME");
                                                if (elements.Count > 0)
                                                {
                                                    xmlSorceFileName = elements[0].InnerText;
                                                    doc.DocumentElement.RemoveChild(elements[0]);
                                                }


                                                elements = doc.GetElementsByTagName("PAY_ID");
                                                if (elements.Count > 0)
                                                {
                                                    InstrId = elements[0].InnerText;
                                                    doc.DocumentElement.RemoveChild(elements[0]);
                                                }


                                                string Tmp1 = $"SELECT TOP 1 Status FROM RTGSBatchTemounsExpec WHERE  AMOUNT = '{Amount}' AND FileName ='{xmlSorceFileName}' AND InstrId ='{InstrId}'";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                string fileStatusCheck = (string)cmd1.ExecuteScalar();

                                                if (fileStatusCheck == "notposted" || fileStatusCheck == "fail")
                                                {
                                                    Tmp1 = $"UPDATE RTGSBatchTemounsExpec SET Status = 'posted', xmlFileName = '{file.Name}', T24DateTime = getdate() WHERE AMOUNT = '{Amount}' AND FileName ='{xmlSorceFileName}' AND InstrId ='{InstrId}';";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();


                                                    Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Pass With status {fileStatusCheck}',getdate(),'Inb');";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();

                                                    doc.Save(file.FullName);
                                                    flag = true;
                                                }
                                                else if (fileStatusCheck == null)
                                                {
                                                    if (File.Exists(RejectedFolder + "//" + file.Name)) File.Delete(RejectedFolder + "//" + file.Name);

                                                    Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Blocked With status Not Expected',getdate(),'Inb');";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();

                                                    File.Move(file.FullName, RejectedFolder + "//" + file.Name);
                                                }
                                                else if (fileStatusCheck == "success")
                                                {
                                                    Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Blocked With status Duplicate',getdate(),'Inb');";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();

                                                    if (File.Exists(RejectedFolder + "//Dup//" + file.Name)) File.Delete(RejectedFolder + "//Dup//" + file.Name);

                                                    File.Move(file.FullName, RejectedFolder + "//Dup//" + file.Name);
                                                }
                                                else if (fileStatusCheck == "returned")
                                                {
                                                    Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Blocked With status Returned',getdate(),'Inb');";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();

                                                    if (File.Exists(RejectedFolder + "//Dup//" + file.Name)) File.Delete(RejectedFolder + "//Dup//" + file.Name);

                                                    File.Move(file.FullName, RejectedFolder + "//Dup//" + file.Name);
                                                }
                                            }
                                            else if (SplitFileName[1] == "IB")
                                            {
                                                flag = false;

                                                File.Copy(file.FullName, RawBackupFolder + "//" + file.Name, true);

                                                sealXmlFile.SealXml(RawBackupFolder + "//" + file.Name);

                                                doc.Load(file.FullName);
                                                string AccountNumber, Amount, InstrId, xmlSorceFileName;

                                                AccountNumber = InstrId = xmlSorceFileName = "N/A";
                                                Amount = "0";

                                                XmlNodeList elements = doc.GetElementsByTagName("CREDIT_ACCT_NO");
                                                if (elements.Count > 0)
                                                {
                                                    AccountNumber = elements[0].InnerText;
                                                }


                                                elements = doc.GetElementsByTagName("DEBIT_AMOUNT");
                                                if (elements.Count > 0)
                                                {
                                                    Amount = elements[0].InnerText;
                                                    string[] ignoreDot = Amount.Split('.');
                                                    if (ignoreDot.Count() > 0)
                                                        Amount = ignoreDot[0];
                                                }


                                                elements = doc.GetElementsByTagName("FILE_NAME");
                                                if (elements.Count > 0)
                                                {
                                                    xmlSorceFileName = elements[0].InnerText;
                                                    doc.DocumentElement.RemoveChild(elements[0]);
                                                }


                                                elements = doc.GetElementsByTagName("PAY_ID");
                                                if (elements.Count > 0)
                                                {
                                                    InstrId = elements[0].InnerText;
                                                    doc.DocumentElement.RemoveChild(elements[0]);
                                                }


                                                string Tmp1 = $"SELECT TOP 1 Status FROM RTGSBatchIBTemounsExpec WHERE  AMOUNT = '{Amount}' AND FileName ='{xmlSorceFileName}' AND InstrId ='{InstrId}'";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                string fileStatusCheck = (string)cmd1.ExecuteScalar();

                                                if (fileStatusCheck == "notposted" || fileStatusCheck == "fail")
                                                {
                                                    Tmp1 = $"UPDATE RTGSBatchIBTemounsExpec SET Status = 'posted', xmlFileName = '{file.Name}', T24DateTime = getdate() WHERE AMOUNT = '{Amount}' AND FileName ='{xmlSorceFileName}' AND InstrId ='{InstrId}';";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();


                                                    Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Pass With status {fileStatusCheck}',getdate(),'InbB');";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();

                                                    doc.Save(file.FullName);
                                                    flag = true;
                                                }
                                                else if (fileStatusCheck == null)
                                                {
                                                    if (File.Exists(RejectedFolder + "//" + file.Name)) File.Delete(RejectedFolder + "//" + file.Name);

                                                    Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Blocked With status Not Expected',getdate(),'InbB');";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();

                                                    File.Move(file.FullName, RejectedFolder + "//" + file.Name);
                                                }
                                                else if (fileStatusCheck == "success")
                                                {
                                                    Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Blocked With status Duplicate',getdate(),'InbB');";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();

                                                    if (File.Exists(RejectedFolder + "//Dup//" + file.Name)) File.Delete(RejectedFolder + "//Dup//" + file.Name);

                                                    File.Move(file.FullName, RejectedFolder + "//Dup//" + file.Name);
                                                }

                                            }
                                            else if (SplitFileName[1] == "TT")
                                            {
                                                flag = false;
                                                doc.Load(file.FullName);
                                                string AccountNumber, Amount, InstrId;

                                                AccountNumber = InstrId = "N/A";
                                                Amount = "0";

                                                File.Copy(file.FullName, RawBackupFolder + "//" + file.Name, true);
                                                sealXmlFile.SealXml(RawBackupFolder + "//" + file.Name);

                                                XmlNodeList elements = doc.GetElementsByTagName("DEBIT_ACCT_NO");
                                                if (elements.Count > 0)
                                                {
                                                    AccountNumber = elements[0].InnerText;
                                                }

                                                if (SplitFileName[2] == "CM")
                                                {
                                                    elements = doc.GetElementsByTagName("OrgCrAcct");
                                                    if (elements.Count > 0)
                                                    {
                                                        AccountNumber = elements[0].InnerText;
                                                        doc.DocumentElement.RemoveChild(elements[0]);
                                                    }
                                                }


                                                elements = doc.GetElementsByTagName("DEBIT_AMOUNT");
                                                if (elements.Count > 0)
                                                {
                                                    Amount = elements[0].InnerText;
                                                    string[] ignoreDot = Amount.Split('.');
                                                    if (ignoreDot.Count() > 0)
                                                        Amount = ignoreDot[0];
                                                }

                                                elements = doc.GetElementsByTagName("InstrId");
                                                if (elements.Count > 0)
                                                {
                                                    InstrId = elements[0].InnerText;
                                                    doc.DocumentElement.RemoveChild(elements[0]);
                                                }

                                                string Tmp1 = $"SELECT Status FROM  RTGSBatchBBExpec WHERE InstrId = '{InstrId}'";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                string fileStatusCheck = (string)cmd1.ExecuteScalar();

                                                if (fileStatusCheck != null)
                                                {

                                                    if (File.Exists(RejectedFolder + "//Dup//" + file.Name)) File.Delete(RejectedFolder + "//Dup//" + file.Name);
                                                    File.Move(file.FullName, RejectedFolder + "//Dup//" + file.Name);
                                                }
                                                else
                                                {

                                                    flag = true;
                                                    doc.Save(file.FullName);

                                                    Tmp1 = $"insert into RTGSBatchBBExpec (FileName, AccountNumber, Amount, Status, initDateTime, InstrId, T24DateTime) VALUES('{file.Name}','{AccountNumber}','{Amount}','posted',getdate(),'{InstrId}',getdate());";
                                                    cmd1 = new SqlCommand(Tmp1, connection);
                                                    cmd1.ExecuteScalar();

                                                }

                                            }

                                            if (flag)
                                            {
                                                string mainFileName = "N/A";

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
                                                handleDuplicate.InsertFile(connection, file.Name, file.FullName, "xmlToRead");


                                                if (SplitFileName[1] == "TT")
                                                {
                                                    //string Tmp1 = $"SELECT isnull(BBFileName,'N/A') FROM RTGS WHERE XMLFileName = '{file.Name}'";
                                                    //SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                    //mainFileName = (string)cmd1.ExecuteScalar();

                                                    // Console.WriteLine("TT Entry :) ");
                                                }
                                                else
                                                {
                                                    string Tmp1 = $"SELECT isnull(FileName,'N/A') FROM XMLDataUpload WHERE XMLFileName = '{NormalFileName}'";
                                                    SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                    mainFileName = (string)cmd1.ExecuteScalar();
                                                    //Console.WriteLine("I Entry :) ");
                                                }

                                                string Tmp = $"INSERT INTO RTGSInwordLog(Date, Remarks, XMLFileName, FileName) VALUES(getdate(), 'File Moved From XML To T24 READ', '{NormalFileName}','{mainFileName}')";
                                                SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                cmd.ExecuteScalar();

                                                AffectedFileCount++;
                                            }
                                            else
                                            {
                                                sw.WriteLine();
                                            }


                                        }
                                        catch (Exception e)
                                        {
                                            sw.WriteLine(e.Message);
                                            Console.WriteLine(e.Message);
                                        }
                                    }
                                    else
                                    {
                                        handleDuplicate.CleanDuplicate(connection, file.FullName, file.Name, "xmlToRead");
                                        DuplicateFileCount++;
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

                Console.WriteLine(DuplicateFileCount.ToString() + ", Duplicate Files For XML TO T24Read");

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
        private readonly XmlDocument doc;
        private readonly HandleDuplicate handleDuplicate;
        private readonly Pack08T24Handle pack08T24Handle;
        private readonly Pack09T24Handle pack09T24Handle;

        public RTGSInbound()
        {
            #region Testing...
            //SourceFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\SATP_IN\From";
            //BackupFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\SATP_IN\Backup";
            //DestinationFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\SATP_IN\To";
            //LogFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\SATP_IN";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion

            #region Deploy...
            SourceFolder = @"D:\distr_STPAdapter_v21_36\output";
            BackupFolder = @"D:\RTGSFiles\SATPToInbound";
            DestinationFolder = @"C:\inetpub\wwwroot\RTGS\Upload\InBoundData";
            LogFolder = @"D:\RTGSFiles\LogFiles\SATPToInbound";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion

            doc = new XmlDocument();
            handleDuplicate = HandleDuplicate.getInstance();
            pack08T24Handle = Pack08T24Handle.getInstance();
            pack09T24Handle = Pack09T24Handle.getInstance();
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
                int DuplicateFileCount = 0;
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
                                    if (!handleDuplicate.IsExixtsFile(connection, file.Name, "outToInbound"))
                                    {
                                        sw.Write(file.FullName);
                                        try
                                        {
                                            doc.Load(file.FullName);
                                            string MsgDefIdr = "N/A";
                                            XmlNodeList elemList = doc.GetElementsByTagName("MsgDefIdr");
                                            if (elemList.Count > 0)
                                            {
                                                MsgDefIdr = elemList[0].InnerText;
                                            }

                                            if (MsgDefIdr == "pacs.008.001.04") pack08T24Handle.InsertAsExpected(connection, file.FullName, file.Name);
                                            else if (MsgDefIdr == "pacs.009.001.04") pack09T24Handle.InsertAsExpected(connection, file.FullName, file.Name);

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

                                            handleDuplicate.InsertFile(connection, file.Name, file.FullName, "outToInbound");

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
                                        handleDuplicate.CleanDuplicate(connection, file.FullName, file.Name, "outToInbound");
                                        DuplicateFileCount++;
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
                Console.WriteLine(DuplicateFileCount.ToString() + ", Duplicate Files For SATP To Inbound");


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
        private readonly string RejectFolder;
        private readonly string ConnectionString;
        private readonly XmlDocument doc;
        private readonly HandleDuplicate handleDuplicate;

        public BBOutBoundData()
        {
            #region Testing...
            //SourceFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\From";
            //BackupFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\Backup";
            //DestinationFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BBOutBound_SATP\To";
            //LogFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\BBOutBound_SATP";
            //RejectFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BatchBBReject";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion

            #region Deploy...
            SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\BBOutBoundData";
            BackupFolder = @"D:\RTGSFiles\BBOutToSATP";
            DestinationFolder = @"D:\distr_STPAdapter_v21_36\input";
            LogFolder = @"D:\RTGSFiles\LogFiles\BBOutToSATP";
            RejectFolder = @"D:\RTGSFiles\BatchBBReject";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion

            handleDuplicate = HandleDuplicate.getInstance();
            doc = new XmlDocument();
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
                int DuplicateFileCount = 0;
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
                                    if (!handleDuplicate.IsExixtsFile(connection, file.Name, "BBOutboudToInput"))
                                    {
                                        sw.Write(file.FullName);
                                        try
                                        {
                                            doc.Load(file.FullName);
                                            string AccountNumber, Amount, InstrId;
                                            string AccountNumberQ, AmountQ, StatusQ, BBxmlFileNameQ, MsgDefIdr;

                                            AccountNumber = InstrId = AccountNumberQ = StatusQ = BBxmlFileNameQ = MsgDefIdr = "N/A";
                                            Amount = AmountQ = "0";

                                            XmlNodeList elements = doc.GetElementsByTagName("DbtrAcct");
                                            if (elements.Count > 0)
                                            {
                                                AccountNumber = elements[0].InnerText;
                                            }

                                            elements = doc.GetElementsByTagName("MsgDefIdr");
                                            if (elements.Count > 0)
                                            {
                                                MsgDefIdr = elements[0].InnerText;
                                            }

                                            if (MsgDefIdr == "pacs.009.001.04")
                                            {
                                                elements = doc.GetElementsByTagName("CdtrAcct");
                                                if (elements.Count > 0)
                                                {
                                                    AccountNumber = elements[0].InnerText;
                                                }
                                            }

                                            elements = doc.GetElementsByTagName("CdtTrfTxInf");

                                            if (MsgDefIdr == "pacs.008.001.04" && elements.Count > 1)
                                            {
                                                elements = doc.GetElementsByTagName("TtlIntrBkSttlmAmt");
                                                if (elements.Count > 0)
                                                {
                                                    Amount = elements[0].InnerText;
                                                    string[] ignoreDot = Amount.Split('.');
                                                    if (ignoreDot.Count() > 0)
                                                        Amount = ignoreDot[0];
                                                }

                                                elements = doc.GetElementsByTagName("MsgId");
                                                if (elements.Count > 0)
                                                {
                                                    InstrId = elements[0].InnerText;
                                                }
                                            }
                                            else
                                            {
                                                elements = doc.GetElementsByTagName("IntrBkSttlmAmt");
                                                if (elements.Count > 0)
                                                {
                                                    Amount = elements[0].InnerText;
                                                    string[] ignoreDot = Amount.Split('.');
                                                    if (ignoreDot.Count() > 0)
                                                        Amount = ignoreDot[0];
                                                }

                                                elements = doc.GetElementsByTagName("InstrId");
                                                if (elements.Count > 0)
                                                {
                                                    InstrId = elements[0].InnerText;
                                                }
                                            }




                                            string MyTmp = $"SELECT TOP(1) AccountNumber, Amount , Status, BBxmlFileName FROM RTGSBatchBBExpec WHERE InstrId = '{InstrId}';";
                                            SqlCommand Mycmd = new SqlCommand(MyTmp, connection);

                                            using (var reader = Mycmd.ExecuteReader())
                                            {
                                                while (reader.Read())
                                                {
                                                    AccountNumberQ = reader.GetString(0);

                                                    AmountQ = reader.GetString(1);
                                                    string[] ignoreDot = AmountQ.Split('.');
                                                    if (ignoreDot.Count() > 0)
                                                        AmountQ = ignoreDot[0];
                                                    StatusQ = reader.GetString(2);
                                                    BBxmlFileNameQ = reader.IsDBNull(3) ? null : reader.GetString(3);

                                                }
                                            }

                                            if (StatusQ == "success" && AccountNumber == AccountNumberQ && Amount == AmountQ && BBxmlFileNameQ == null)
                                            {
                                                string Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File pass With status Not posted To BB',getdate(),'BB');";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

                                                Tmp1 = $"UPDATE RTGSBatchBBExpec SET BBxmlFileName = '{file.Name}' WHERE InstrId = '{InstrId}';";
                                                cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

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
                                                handleDuplicate.InsertFile(connection, file.Name, file.FullName, "BBOutboudToInput");
                                                AffectedFileCount++;
                                            }
                                            else if (StatusQ == null)
                                            {
                                                sw.WriteLine();
                                                string Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File block With status Not listed for BB',getdate(),'BB');";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

                                                if (File.Exists(RejectFolder + "//" + file.Name)) File.Delete(RejectFolder + "//" + file.Name);
                                                File.Move(file.FullName, RejectFolder + "//" + file.Name);

                                            }
                                            else if (StatusQ == "fail")
                                            {
                                                sw.WriteLine();
                                                string Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File block With status Temonus Fail',getdate(),'BB');";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

                                                if (File.Exists(RejectFolder + "//" + file.Name)) File.Delete(RejectFolder + "//" + file.Name);
                                                File.Move(file.FullName, RejectFolder + "//" + file.Name);
                                            }
                                            else if (BBxmlFileNameQ == "N/A")
                                            {
                                                sw.WriteLine();
                                                string Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File block With status Not Expected To BB',getdate(),'BB');";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

                                                if (File.Exists(RejectFolder + "//" + file.Name)) File.Delete(RejectFolder + "//" + file.Name);
                                                File.Move(file.FullName, RejectFolder + "//" + file.Name);
                                            }
                                            else if (BBxmlFileNameQ != null)
                                            {
                                                sw.WriteLine();
                                                string Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File block With status Already posted To BB',getdate(),'BB');";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

                                                if (File.Exists(RejectFolder + "//" + file.Name)) File.Delete(RejectFolder + "//" + file.Name);
                                                File.Move(file.FullName, RejectFolder + "//" + file.Name);

                                            }
                                            else if (AccountNumber != AccountNumberQ || Amount != AmountQ)
                                            {
                                                sw.WriteLine();
                                                string Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File block With status Account Or Amount Mitchmatch',getdate(),'BB');";
                                                SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

                                                if (File.Exists(RejectFolder + "//" + file.Name)) File.Delete(RejectFolder + "//" + file.Name);
                                                File.Move(file.FullName, RejectFolder + "//" + file.Name);
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
                                        handleDuplicate.CleanDuplicate(connection, file.FullName, file.Name, "BBOutboudToInput");
                                        DuplicateFileCount++;
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
                Console.WriteLine(DuplicateFileCount.ToString() + ", Duplicate Files For BBOut To Input");

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
        private readonly string RejectedFolder;
        private readonly string ConnectionString;
        private readonly XmlDocument doc;
        private readonly HandleDuplicate handleDuplicate;
        public RTGSReturn()
        {
            #region  Testing...
            SourceFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\Return_READ\From";
            BackupFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\Return_READ\Backup";
            DestinationFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\Return_READ\To";
            LogFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\Return_IN";
            RejectedFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BatchReturnReject";
            ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion

            #region Deploy...
            //SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\ReturnInBound";
            //BackupFolder = @"D:\RTGSFiles\ReturnInboundToInput";
            //DestinationFolder = @"D:\distr_STPAdapter_v21_36\input";
            //LogFolder = @"D:\RTGSFiles\LogFiles\ReturnInboundToInput";
            //RejectedFolder = @"";
            //ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion

            handleDuplicate = HandleDuplicate.getInstance();
            doc = new XmlDocument();
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
                int DuplicateFileCount = 0;
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
                                    if (!handleDuplicate.IsExixtsFile(connection, file.Name, "ReturnToInput"))
                                    {
                                        sw.Write(file.FullName);
                                        try
                                        {
                                            doc.Load(file.FullName);
                                            string Amount, InstrId;
                                            Amount = InstrId = "N/A";

                                            XmlNodeList elements = doc.GetElementsByTagName("OrgnlInstrId");
                                            if (elements.Count > 0)
                                            {
                                                InstrId = elements[0].InnerText;
                                            }


                                            elements = doc.GetElementsByTagName("RtrdIntrBkSttlmAmt");
                                            if (elements.Count > 0)
                                            {
                                                Amount = elements[0].InnerText;
                                                string[] ignoreDot = Amount.Split('.');
                                                if (ignoreDot.Count() > 0)
                                                    Amount = ignoreDot[0];
                                            }

                                            string Tmp1 = $"SELECT TOP 1 Status FROM RTGSBatchTemounsExpec  WHERE Amount= '{Amount}' AND InstrId = '{InstrId}';";
                                            SqlCommand cmd1 = new SqlCommand(Tmp1, connection);
                                            string fileStatusCheck = (string)cmd1.ExecuteScalar();


                                            if (fileStatusCheck == "notposted" || fileStatusCheck == "fail")
                                            {
                                                Tmp1 = $"UPDATE RTGSBatchTemounsExpec SET Status = 'returned' WHERE Amount= '{Amount}' AND InstrId = '{InstrId}';";
                                                cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

                                                Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Pass With status {fileStatusCheck}',getdate(),'InbR');";
                                                cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();


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

                                                handleDuplicate.InsertFile(connection, file.Name, file.FullName, "ReturnToInput");

                                                AffectedFileCount++;
                                            }
                                            else
                                            {
                                                sw.Write(" | Unexpected File");
                                                sw.WriteLine();

                                                Tmp1 = $"INSERT INTO RTGSBatchGetKeeperLog (FileName,Remarks,DateTime,Type) VALUES('{file.Name}','File Block With status Not expected',getdate(),'InbR');";
                                                 cmd1 = new SqlCommand(Tmp1, connection);
                                                cmd1.ExecuteScalar();

                                                if (File.Exists(RejectedFolder + "//" + file.Name)) File.Delete(RejectedFolder + "//" + file.Name);

                                                File.Move(file.FullName, RejectedFolder + "//" + file.Name);

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
                                        handleDuplicate.CleanDuplicate(connection, file.FullName, file.Name, "ReturnToInput");
                                        DuplicateFileCount++;
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
                Console.WriteLine(DuplicateFileCount.ToString() + ", Duplicate Files For Return To Input");

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
        private readonly HandleDuplicate handleDuplicate;

        public RTGSStatusUpdate()
        {

            #region Testing...  
            //LogFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\RTGSStatus";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            //SourceFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\RTGSStatus\Source";//Assuming Test is your Folder
            //BackupFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\RTGSStatus\backup";
            #endregion

            #region Deploy...
            LogFolder = @"D:\RTGSFiles\LogFiles\T24StatusUpdate";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            SourceFolder = @"X:\AGR.WRITE";//Assuming Test is your Folder
            BackupFolder = @"D:\RTGSFiles\T24WriteBackup";
            #endregion

            handleDuplicate = HandleDuplicate.getInstance();

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
                                                    string myTemp = $"UPDATE RTGSBatchTemounsExpec SET Status='success', SuccessDate = getdate() WHERE xmlFileName = '{file.Name}';";
                                                    SqlCommand Mycmd = new SqlCommand(myTemp, connection);
                                                    Mycmd.ExecuteScalar();

                                                    cmdTm.Parameters.AddWithValue("@Remarks ", "Success");
                                                }
                                                else
                                                {
                                                    string myTemp = $"SELECT TOP 1 Status FROM  RTGSBatchTemounsExpec WHERE xmlFileName = '{file.Name}'";
                                                    SqlCommand Mycmd = new SqlCommand(myTemp, connection);
                                                    string MyStatus = (string)Mycmd.ExecuteScalar();

                                                    if (MyStatus != "success" && MyStatus != "returned")
                                                    {
                                                        myTemp = $"UPDATE RTGSBatchTemounsExpec SET Status='fail' WHERE xmlFileName = '{file.Name}';";
                                                        Mycmd = new SqlCommand(myTemp, connection);
                                                        Mycmd.ExecuteScalar();
                                                    }
                                                    cmdTm.Parameters.AddWithValue("@Remarks ", "Transiction Fail");
                                                }
                                                cmdTm.ExecuteScalar();

                                                // Console.WriteLine("I");
                                            }
                                            else if (SplitFileName[1] == "IB")
                                            {
                                                string Tmp = $"UPDATE BankToBankBorrow SET TrStatus = '{Status}', TraNumber = '{TranNumber}', ErrDescription = '{TranNumber}' WHERE XMLFileName = '{NormalFileName}'";
                                                SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                cmd.ExecuteScalar();

                                                if (Status == "1")
                                                {
                                                    string myTemp = $"UPDATE RTGSBatchIBTemounsExpec SET Status='success', SuccessDate = getdate() WHERE xmlFileName = '{file.Name}';";
                                                    SqlCommand Mycmd = new SqlCommand(myTemp, connection);
                                                    Mycmd.ExecuteScalar();


                                                }
                                                else
                                                {
                                                    string myTemp = $"SELECT TOP 1 Status FROM  RTGSBatchIBTemounsExpec WHERE xmlFileName = '{file.Name}'";
                                                    SqlCommand Mycmd = new SqlCommand(myTemp, connection);
                                                    string MyStatus = (string)Mycmd.ExecuteScalar();

                                                    if (MyStatus != "success")
                                                    {
                                                        myTemp = $"UPDATE RTGSBatchIBTemounsExpec SET Status='fail' WHERE xmlFileName = '{file.Name}';";
                                                        Mycmd = new SqlCommand(myTemp, connection);
                                                        Mycmd.ExecuteScalar();
                                                    }
                                                }

                                                // Console.WriteLine("IB");
                                            }
                                            else if (SplitFileName[1] == "TT")
                                            {
                                                string Tmp = $"UPDATE dbo.RTGS SET TraNumber = '{TranNumber}', TrStatus = '{Status}', ErrDescription = '{ErrMessage}' WHERE XMLFileName = '{FileName}'";
                                                SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                cmd.ExecuteScalar();

                                                if (Status == "1")
                                                {
                                                    string myTemp = $"UPDATE RTGSBatchBBExpec SET Status = 'success',SuccessDate = getdate() WHERE FileName='{file.Name}';";
                                                    SqlCommand Mycmd = new SqlCommand(myTemp, connection);
                                                    Mycmd.ExecuteScalar();
                                                }
                                                else
                                                {
                                                    string myTemp = $"UPDATE RTGSBatchBBExpec SET Status = 'fail' WHERE FileName='{file.Name}';";
                                                    SqlCommand Mycmd = new SqlCommand(myTemp, connection);
                                                    Mycmd.ExecuteScalar();
                                                }


                                                SqlCommand cmdTm = new SqlCommand("SP_Post_Rtgs_StatusLog_By_Batch", connection);
                                                cmdTm.CommandType = CommandType.StoredProcedure;
                                                cmdTm.Parameters.AddWithValue("@FileName", file.Name);
                                                cmdTm.Parameters.AddWithValue("@StatusID", Status);
                                                cmdTm.Parameters.AddWithValue("@ProcessType", "TT");
                                                cmdTm.ExecuteScalar();

                                                if (SplitFileName[2] == "IO")
                                                {
                                                    string myTemp = $"UPDATE ReturnRTGS SET TrStatus = '{Status}' WHERE FileName = '{file.Name}';";
                                                    SqlCommand Mycmd = new SqlCommand(myTemp, connection);
                                                    Mycmd.ExecuteScalar();
                                                }
                                                else if (SplitFileName[2] == "CM")
                                                {
                                                    string myTemp = $"UPDATE CallMoney SET TraNumber = '{TranNumber}', TrStatus = '{Status}', ErrDescription = '{ErrMessage}' WHERE XMLFileName = '{FileName}'";
                                                    SqlCommand Mycmd = new SqlCommand(myTemp, connection);
                                                    Mycmd.ExecuteScalar();
                                                }

                                                //Console.WriteLine("TT");
                                            }

                                            sw.Write(file.FullName);
                                            File.Copy(file.FullName, BackupFolder + "\\" + file.Name, true);
                                            sw.Write(" | Updated successfully");
                                            sw.WriteLine();

                                            handleDuplicate.InsertUpdateFile(connection, file.Name, "T24WRITE");
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
        private readonly HandleDuplicate handleDuplicate;
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

            handleDuplicate = HandleDuplicate.getInstance();
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

                                        if (SplitFileName.Length > 2 && SplitFileName[1] == "BB" && SplitFileName[2] != "CM")
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
                                        else if (SplitFileName.Length > 2 && SplitFileName[1] == "BB" && SplitFileName[2] == "CM")
                                        {
                                            string Tmp = $"UPDATE CallMoney SET BBTraNumber = '{resultData}', BBErrDescription = 'N/A', BBTrStatus = '1' WHERE BBFileName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
                                        }

                                        //Aditional Logic

                                        handleDuplicate.InsertUpdateFile(connection, file.Name, "SATPAckErr");

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

                                        if (SplitFileName.Length > 2 && SplitFileName[1] == "BB" && SplitFileName[2] != "CM")
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
                                        else if (SplitFileName.Length > 2 && SplitFileName[1] == "BB" && SplitFileName[2] == "CM")
                                        {
                                            string Tmp = $"UPDATE CallMoney SET BBTraNumber = 'N/A', BBErrDescription = '{ErrMessage}', BBTrStatus = '-1' WHERE BBFileName = '{NormalizeFileName}'";
                                            SqlCommand cmd = new SqlCommand(Tmp, connection);
                                            cmd.ExecuteScalar();
                                        }

                                        //Aditional Logic
                                        handleDuplicate.InsertUpdateFile(connection, file.Name, "SATPAckErr");

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
        private readonly HandleDuplicate handleDuplicate;
        public InboundFileProcess()
        {
            #region Testing...
            //SourceFolder = @"E:\Development\Jogessor\newfile\InBoundData";
            //LogFolder = @"E:\Development\Jogessor\2018-12-25\RTGS\BackUpRTGSInWordLogFiles\InboundFileProcessLog";
            //ConnectionString = @"Data Source=.;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@1234;Pooling=true;Max Pool Size=32700;Integrated Security=True";
            #endregion


            #region Deploy...
            SourceFolder = @"C:\inetpub\wwwroot\RTGS\Upload\InBoundData";
            LogFolder = @"D:\RTGSFiles\LogFiles\RTGSFileProcess";
            ConnectionString = @"Data Source=WIN-7HGA9A6FBHT;Initial Catalog=db_ABL_RTGS;User ID=sa;Password=sa@123; Pooling=true;Max Pool Size=32700;";
            #endregion
            handleDuplicate = HandleDuplicate.getInstance();
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
                    string FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, CdtNbOfNtries, DbtNbOfNtries, CdtSum, DbtSum, CdtrAcct, CdtDbtInd, OrgnlMsgId, AddtlInf, EndToEndId, TxId, DbtrAcct, DbtrFinInstnId, DbtrBrnchId, CdtrFinInstnId, CdtrBrnchId;
                    XmlDocument doc = new XmlDocument();

                    if (files.Count() != 0)
                    {
                        using (SqlConnection connection = new SqlConnection(ConnectionString))
                        {
                            connection.Open();
                            foreach (FileInfo file in files)
                            {
                                FileName = MsgDefIdr = BizMsgIdr = CreDt = DebDt = AcctId = NtryRef = InstrId = AnyBIC = OrgnlInstrId = CdtNbOfNtries = DbtNbOfNtries = CdtSum = DbtSum = CdtrAcct = CdtDbtInd = OrgnlMsgId = AddtlInf = EndToEndId = TxId = DbtrAcct = DbtrFinInstnId = DbtrBrnchId = CdtrFinInstnId = CdtrBrnchId = "N/A";
                                Amt = "0";

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
                                                if (TempName == "camt054")//For camt054
                                                {
                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");
                                                    AcctId = InerTextOfTag(doc, "Acct");
                                                    NtryRef = InerTextOfTag(doc, "NtryRef");
                                                    InstrId = InerTextOfTag(doc, "InstrId");
                                                    AnyBIC = InerTextOfTag(doc, "AnyBIC");
                                                    Amt = InerTextOfTag(doc, "Amt");

                                                    string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, DateTime)  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}', getdate());";
                                                    SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                    cmd.ExecuteScalar();

                                                }
                                                else if (TempName == "camt052")
                                                {
                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");
                                                    AcctId = InerTextOfTag(doc, "Acct");
                                                    NtryRef = InerTextOfTag(doc, "NtryRef");
                                                    InstrId = InerTextOfTag(doc, "InstrId");
                                                    AnyBIC = InerTextOfTag(doc, "AnyBIC");

                                                    XmlNodeList TMnodes = doc.GetElementsByTagName("TtlCdtNtries");

                                                    if (TMnodes.Count > 0)
                                                    {
                                                        for (int i = 0; i < TMnodes[0].ChildNodes.Count; i++)
                                                            if (TMnodes[0].ChildNodes[i].Name == "NbOfNtries") CdtNbOfNtries = TMnodes[0].ChildNodes[i].InnerText;
                                                            else if (TMnodes[0].ChildNodes[i].Name == "Sum") CdtSum = TMnodes[0].ChildNodes[i].InnerText;
                                                    }

                                                    TMnodes = doc.GetElementsByTagName("TtlDbtNtries");

                                                    if (TMnodes.Count > 0)
                                                    {
                                                        for (int i = 0; i < TMnodes[0].ChildNodes.Count; i++)
                                                            if (TMnodes[0].ChildNodes[i].Name == "NbOfNtries") DbtNbOfNtries = TMnodes[0].ChildNodes[i].InnerText;
                                                            else if (TMnodes[0].ChildNodes[i].Name == "Sum") DbtSum = TMnodes[0].ChildNodes[i].InnerText;
                                                    }

                                                    XmlNodeList nodes = doc.GetElementsByTagName("Amt");
                                                    foreach (XmlNode node in nodes)
                                                    {
                                                        Amt = node.InnerText;
                                                        string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, DateTime, CdtNbOfNtries, CdtSum, DbtNbOfNtries, DbtSum )  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}', getdate(), '{CdtNbOfNtries}', '{CdtSum}','{DbtNbOfNtries}','{DbtSum}');";
                                                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                        cmd.ExecuteScalar();
                                                    }
                                                }
                                                else if (TempName == "camt053")//For Camt053
                                                {
                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");
                                                    AcctId = InerTextOfTag(doc, "Acct");
                                                    InstrId = InerTextOfTag(doc, "InstrId");
                                                    AnyBIC = InerTextOfTag(doc, "AnyBIC");

                                                    XmlNodeList NtryelemList = doc.GetElementsByTagName("Ntry");

                                                    foreach (XmlNode node in NtryelemList)
                                                    {
                                                        NtryRef = CdtDbtInd = "N/A";
                                                        Amt = "0.0";

                                                        for (int i = 0; i < node.ChildNodes.Count; i++)
                                                        {
                                                            if (node.ChildNodes[i].Name == "NtryRef") NtryRef = node.ChildNodes[i].InnerText;
                                                            else if (node.ChildNodes[i].Name == "Amt") Amt = node.ChildNodes[i].InnerText;
                                                            else if (node.ChildNodes[i].Name == "CdtDbtInd") CdtDbtInd = node.ChildNodes[i].InnerText;
                                                            else if (node.ChildNodes[i].Name == "NtryDtls")
                                                            {
                                                                foreach (XmlNode node1 in node)
                                                                    if (node1.Name == "TxDtls")
                                                                        foreach (XmlNode node2 in node1)
                                                                            if (node2.Name == "Refs")
                                                                                foreach (XmlNode node3 in node2)
                                                                                    if (node3.Name == "InstrId") InstrId = node3.InnerText;
                                                            }
                                                        }

                                                        string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, CdtDbtInd, DateTime)  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}' , '{CdtDbtInd}' , getdate());";
                                                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                        cmd.ExecuteScalar();

                                                    }

                                                    if (NtryelemList.Count == 0)
                                                    {
                                                        string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, DateTime)  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}', getdate());";
                                                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                        cmd.ExecuteScalar();
                                                    }
                                                }
                                                else if (TempName == "pacs002")//pacs.002
                                                {
                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");
                                                    OrgnlInstrId = InerTextOfTag(doc, "OrgnlInstrId");
                                                    OrgnlMsgId = InerTextOfTag(doc, "OrgnlMsgId");
                                                    Amt = InerTextOfTag(doc, "IntrBkSttlmAmt");
                                                    CdtrAcct = InerTextOfTag(doc, "CdtrAcct");
                                                    AddtlInf = InerTextOfTag(doc, "AddtlInf");

                                                    string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, CdtrAcct,OrgnlMsgId,AddtlInf, DateTime)  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}', '{CdtrAcct}','{OrgnlMsgId}', '{AddtlInf}', getdate());";
                                                    SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                    cmd.ExecuteScalar();

                                                }
                                                else if (TempName == "camt025")
                                                {


                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");


                                                    XmlNodeList MyelemList = doc.GetElementsByTagName("OrgnlMsgId");
                                                    if (MyelemList.Count > 0)
                                                    {

                                                        for (int i = 0; i < MyelemList[0].ChildNodes.Count; i++)
                                                            if (MyelemList[0].ChildNodes[i].Name == "MsgId") OrgnlMsgId = MyelemList[0].ChildNodes[i].InnerText;
                                                    }

                                                    MyelemList = doc.GetElementsByTagName("ReqHdlg");
                                                    if (MyelemList.Count > 0)
                                                    {

                                                        for (int i = 0; i < MyelemList[0].ChildNodes.Count; i++)
                                                            if (MyelemList[0].ChildNodes[i].Name == "Desc") AddtlInf = MyelemList[0].ChildNodes[i].InnerText;
                                                    }

                                                    string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, CdtrAcct,OrgnlMsgId,AddtlInf, DateTime)  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}', '{CdtrAcct}','{OrgnlMsgId}', '{AddtlInf}', getdate());";
                                                    SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                    cmd.ExecuteScalar();

                                                }
                                                else if (TempName == "pacs004")//pacs.004
                                                {
                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");
                                                    OrgnlInstrId = InerTextOfTag(doc, "OrgnlInstrId");
                                                    OrgnlMsgId = InerTextOfTag(doc, "OrgnlMsgId");
                                                    Amt = InerTextOfTag(doc, "IntrBkSttlmAmt");
                                                    CdtrAcct = InerTextOfTag(doc, "CdtrAcct");
                                                    AddtlInf = InerTextOfTag(doc, "AddtlInf");
                                                    string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, CdtrAcct, OrgnlMsgId, AddtlInf, DateTime)  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}', '{CdtrAcct}', '{OrgnlMsgId}','{AddtlInf}', getdate());";
                                                    SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                    cmd.ExecuteScalar();

                                                }
                                                else if (TempName == "pacs009")//pacs.004
                                                {
                                                    BizMsgIdr = InerTextOfTag(doc, "BizMsgIdr");
                                                    CreDt = InerTextOfTag(doc, "CreDt");
                                                    DebDt = InerTextOfTag(doc, "DebDt");
                                                    InstrId = InerTextOfTag(doc, "InstrId");
                                                    Amt = InerTextOfTag(doc, "IntrBkSttlmAmt");
                                                    EndToEndId = InerTextOfTag(doc, "EndToEndId");
                                                    TxId = InerTextOfTag(doc, "TxId");
                                                    CdtrAcct = InerTextOfTag(doc, "CdtrAcct");
                                                    DbtrAcct = InerTextOfTag(doc, "DbtrAcct");

                                                    XmlNodeList myelemList = doc.GetElementsByTagName("Dbtr");
                                                    if (myelemList.Count > 0)
                                                    {
                                                        for (int i = 0; i < myelemList[0].ChildNodes.Count; i++)
                                                            if (myelemList[0].ChildNodes[i].Name == "FinInstnId") DbtrFinInstnId = myelemList[0].ChildNodes[i].InnerText;
                                                            else if (myelemList[0].ChildNodes[i].Name == "BrnchId") DbtrBrnchId = myelemList[0].ChildNodes[i].InnerText;

                                                    }

                                                    myelemList = doc.GetElementsByTagName("Cdtr");
                                                    if (myelemList.Count > 0)
                                                    {
                                                        for (int i = 0; i < myelemList[0].ChildNodes.Count; i++)
                                                            if (myelemList[0].ChildNodes[i].Name == "FinInstnId") CdtrFinInstnId = myelemList[0].ChildNodes[i].InnerText;
                                                            else if (myelemList[0].ChildNodes[i].Name == "BrnchId") CdtrBrnchId = myelemList[0].ChildNodes[i].InnerText;
                                                    }

                                                    string Tmp = $"insert into InboundDataBatch (FileName, MsgDefIdr, BizMsgIdr, CreDt, DebDt, Amt, AcctId, NtryRef, InstrId, AnyBIC, OrgnlInstrId, CdtrAcct, OrgnlMsgId, AddtlInf, EndToEndId, TxId, DbtrAcct, DbtrFinInstnId, DbtrBrnchId, CdtrFinInstnId, CdtrBrnchId, DateTime)  VALUES('{file.Name}', '{MsgDefIdr}', '{BizMsgIdr}', '{CreDt}', '{DebDt}', '{Amt}', '{AcctId}', '{NtryRef}', '{InstrId}', '{AnyBIC}', '{OrgnlInstrId}', '{CdtrAcct}', '{OrgnlMsgId}','{AddtlInf}','{EndToEndId}', '{TxId}', '{DbtrAcct}','{DbtrFinInstnId}', '{DbtrBrnchId}','{CdtrFinInstnId}', '{CdtrBrnchId}', getdate());";
                                                    SqlCommand cmd = new SqlCommand(Tmp, connection);
                                                    cmd.ExecuteScalar();
                                                }
                                                else
                                                {
                                                    //Console.WriteLine(MsgDefIdr);
                                                    continue;
                                                }

                                                sw.WriteLine(file.FullName);


                                                handleDuplicate.InsertUpdateFile(connection, file.Name, "InboundFile");
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
            if (tagName == "IntrBkSttlmAmt") return "0";
            return "N/A";
        }

    }

    public class HandleDuplicate
    {
        private readonly string xmlToREAD;
        private readonly string outToInbound;
        private readonly string BBOutboudToInput;
        private readonly string ReturnToInput;

        private static HandleDuplicate instance = new HandleDuplicate();

        public HandleDuplicate()
        {
            #region Testing...
            //xmlToREAD = @"E:\Development\Jogessor\2018-12-25\RTGS\Duplicate\xmlToREAD";
            //outToInbound = @"E:\Development\Jogessor\2018-12-25\RTGS\Duplicate\outToInbound";
            //BBOutboudToInput = @"E:\Development\Jogessor\2018-12-25\RTGS\Duplicate\BBOutboudToInput";
            //ReturnToInput = @"E:\Development\Jogessor\2018-12-25\RTGS\Duplicate\ReturnToInput";
            #endregion

            #region Deploy
            xmlToREAD = @"D:\RTGSFiles\Duplicate\xmlToREAD";
            outToInbound = @"D:\RTGSFiles\Duplicate\outToInbound";
            BBOutboudToInput = @"D:\RTGSFiles\Duplicate\BBOutToInput";
            ReturnToInput = @"D:\RTGSFiles\Duplicate\ReturnToInput";
            #endregion

        }

        public void InsertUpdateFile(SqlConnection connection, string fileName, string Type)
        {
            if (Type == "T24WRITE")
            {
                string Tmp = $"insert into RTGSBatchFileUpdateLog (FileName, Type, DateTime) VALUES('{fileName}','T24WRITE',getdate());";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                cmd.ExecuteScalar();
            }
            else if (Type == "SATPAckErr")
            {
                string Tmp = $"insert into RTGSBatchFileUpdateLog (FileName, Type, DateTime) VALUES('{fileName}','SATPAckErr',getdate());";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                cmd.ExecuteScalar();
            }
            else if (Type == "InboundFile")
            {
                string Tmp = $"insert into RTGSBatchFileUpdateLog (FileName, Type, DateTime) VALUES('{fileName}','InboundFile',getdate());";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                cmd.ExecuteScalar();
            }
        }
        public void InsertFile(SqlConnection connection, string fileName, string FullFileName, string Type)
        {
            if (Type == "xmlToRead")
            {
                string Tmp = $"insert into RTGSBatchFileLog (FileName, Type, DateTime) VALUES('{fileName}','xmlToRead',getdate());";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                cmd.ExecuteScalar();
            }
            else if (Type == "outToInbound")
            {
                string Tmp = $"insert into RTGSBatchFileLog (FileName, Type, DateTime) VALUES('{fileName}','outToInbound',getdate());";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                cmd.ExecuteScalar();
            }
            else if (Type == "BBOutboudToInput")
            {
                string Tmp = $"insert into RTGSBatchFileLog (FileName, Type, DateTime) VALUES('{fileName}','BBOutboudToInput',getdate());";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                cmd.ExecuteScalar();
            }
            else if (Type == "ReturnToInput")
            {
                string Tmp = $"insert into RTGSBatchFileLog (FileName, Type, DateTime) VALUES('{fileName}','ReturnToInput',getdate());";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                cmd.ExecuteScalar();
            }
        }

        public bool IsExixtsFile(SqlConnection connection, string fileName, string Type)
        {
            string result = "";
            if (Type == "xmlToRead")
            {
                string Tmp = $"SELECT FileName FROM RTGSBatchFileLog WHERE Type = 'xmlToRead' AND FileName='{fileName}'";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                result = (string)cmd.ExecuteScalar();
            }
            else if (Type == "outToInbound")
            {
                string Tmp = $"SELECT FileName FROM RTGSBatchFileLog WHERE Type = 'outToInbound' AND FileName='{fileName}'";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                result = (string)cmd.ExecuteScalar();
            }
            else if (Type == "BBOutboudToInput")
            {
                string Tmp = $"SELECT FileName FROM RTGSBatchFileLog WHERE Type = 'BBOutboudToInput' AND FileName='{fileName}'";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                result = (string)cmd.ExecuteScalar();
            }
            else if (Type == "ReturnToInput")
            {
                string Tmp = $"SELECT FileName FROM RTGSBatchFileLog WHERE Type = 'ReturnToInput' AND FileName='{fileName}'";
                SqlCommand cmd = new SqlCommand(Tmp, connection);
                result = (string)cmd.ExecuteScalar();
            }

            if (result != null) return true;

            return false;
        }

        public void CleanDuplicate(SqlConnection connection, string FileFullName, string fileName, string Type)
        {
            try
            {
                if (Directory.Exists(xmlToREAD) && Directory.Exists(outToInbound) && Directory.Exists(BBOutboudToInput) && Directory.Exists(ReturnToInput))
                {
                    if (Type == "xmlToRead")
                    {
                        int f = 1;
                        string tempFilename = fileName;
                        while (File.Exists(xmlToREAD + "/" + tempFilename))
                        {
                            tempFilename = (fileName + "." + f.ToString());
                            f++;
                        }
                        fileName = tempFilename;

                        File.Move(FileFullName, xmlToREAD + "/" + fileName);

                        string Tmp = $"INSERT INTO RTGSBatchFileDuplicateLog (FileName, Type, DateTime) VALUES ('{fileName}', 'xmlToRead', getdate());";
                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                        cmd.ExecuteScalar();

                    }
                    else if (Type == "outToInbound")
                    {
                        int f = 1;
                        string tempFilename = fileName;
                        while (File.Exists(outToInbound + "/" + tempFilename))
                        {
                            tempFilename = (fileName + "." + f.ToString());
                            f++;
                        }
                        fileName = tempFilename;

                        File.Move(FileFullName, outToInbound + "/" + fileName);

                        string Tmp = $"INSERT INTO RTGSBatchFileDuplicateLog (FileName, Type, DateTime) VALUES ('{fileName}', 'outToInbound', getdate());";
                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                        cmd.ExecuteScalar();

                    }
                    else if (Type == "BBOutboudToInput")
                    {
                        int f = 1;
                        string tempFilename = fileName;
                        while (File.Exists(BBOutboudToInput + "/" + tempFilename))
                        {
                            tempFilename = (fileName + "." + f.ToString());
                            f++;
                        }
                        fileName = tempFilename;

                        File.Move(FileFullName, BBOutboudToInput + "/" + fileName);

                        string Tmp = $"INSERT INTO RTGSBatchFileDuplicateLog (FileName, Type, DateTime) VALUES ('{fileName}', 'BBOutboudToInput', getdate());";
                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                        cmd.ExecuteScalar();

                    }
                    else if (Type == "ReturnToInput")
                    {
                        int f = 1;
                        string tempFilename = fileName;
                        while (File.Exists(ReturnToInput + "/" + tempFilename))
                        {
                            tempFilename = (fileName + "." + f.ToString());
                            f++;
                        }
                        fileName = tempFilename;

                        File.Move(FileFullName, ReturnToInput + "/" + fileName);

                        string Tmp = $"INSERT INTO RTGSBatchFileDuplicateLog (FileName, Type, DateTime) VALUES ('{fileName}', 'ReturnToInput', getdate());";
                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                        cmd.ExecuteScalar();
                    }
                }
                else
                {
                    Console.WriteLine("Some Of the directory can't found so operation ignored");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Can't find Directory For Duplicate Handle");
            }
        }

        public static HandleDuplicate getInstance()
        {
            return instance;
        }
    }

    public class Pack08T24Handle
    {
        private readonly XmlDocument doc;
        private static Pack08T24Handle instance = new Pack08T24Handle();
        public Pack08T24Handle()
        {
            doc = new XmlDocument();
        }
        public void InsertAsExpected(SqlConnection connection, string FullFileName, string fileName)
        {
            string AccoutNumber = "N/A";
            string amount = "0";
            string InstrId = "N/A";
            try
            {
                doc.Load(FullFileName);
                XmlNodeList elements = doc.GetElementsByTagName("CdtTrfTxInf");

                foreach (XmlNode element in elements)
                {
                    AccoutNumber = "N/A";
                    amount = "0";
                    InstrId = "N/A";

                    for (int i = 0; i < element.ChildNodes.Count; i++)
                    {
                        if (element.ChildNodes[i].Name == "PmtId")
                        {

                            foreach (XmlNode node in element.ChildNodes[i])
                            {
                                if (node.Name == "InstrId") InstrId = node.InnerText;
                            }
                        }
                        else if (element.ChildNodes[i].Name == "IntrBkSttlmAmt")
                        {
                            amount = element.ChildNodes[i].InnerText;
                            string[] ignoreDot = amount.Split('.');
                            if (ignoreDot.Count() > 0)
                                amount = ignoreDot[0];
                        }
                        else if (element.ChildNodes[i].Name == "CdtrAcct")
                        {
                            AccoutNumber = element.ChildNodes[i].InnerText;
                        }
                    }

                    if (AccoutNumber != "N/A" && InstrId != "N/A")
                    {
                        string Tmp = $"INSERT INTO RTGSBatchTemounsExpec (FileName,AccountNumber,Amount,Status, initDateTime, InstrId) VALUES('{fileName}','{AccoutNumber}','{amount}','notposted',getdate(),'{InstrId}');";
                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                        cmd.ExecuteScalar();
                    }

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        public static Pack08T24Handle getInstance()
        {
            return instance;
        }
    }

    public class Pack09T24Handle
    {
        private readonly XmlDocument doc;
        private static Pack09T24Handle instance = new Pack09T24Handle();
        public Pack09T24Handle()
        {
            doc = new XmlDocument();
        }
        public void InsertAsExpected(SqlConnection connection, string FullFileName, string fileName)
        {
            string AccoutNumber = "N/A";
            string amount = "0";
            string InstrId = "N/A";
            try
            {
                doc.Load(FullFileName);
                XmlNodeList elements = doc.GetElementsByTagName("CdtTrfTxInf");

                foreach (XmlNode element in elements)
                {
                    AccoutNumber = "N/A";
                    amount = "0";
                    InstrId = "N/A";

                    for (int i = 0; i < element.ChildNodes.Count; i++)
                    {
                        if (element.ChildNodes[i].Name == "PmtId")
                        {

                            foreach (XmlNode node in element.ChildNodes[i])
                            {
                                if (node.Name == "InstrId") InstrId = node.InnerText;
                            }
                        }
                        else if (element.ChildNodes[i].Name == "IntrBkSttlmAmt")
                        {
                            amount = element.ChildNodes[i].InnerText;
                            string[] ignoreDot = amount.Split('.');
                            if (ignoreDot.Count() > 0)
                                amount = ignoreDot[0];
                        }
                        else if (element.ChildNodes[i].Name == "CdtrAcct")
                        {
                            AccoutNumber = element.ChildNodes[i].InnerText;
                        }
                    }

                    if (AccoutNumber != "N/A" && InstrId != "N/A")
                    {
                        string Tmp = $"INSERT INTO RTGSBatchIBTemounsExpec (FileName,AccountNumber,Amount,Status, initDateTime, InstrId) VALUES('{fileName}','{AccoutNumber}','{amount}','notposted',getdate(),'{InstrId}');";
                        SqlCommand cmd = new SqlCommand(Tmp, connection);
                        cmd.ExecuteScalar();
                    }

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public static Pack09T24Handle getInstance()
        {
            return instance;
        }

    }

}
