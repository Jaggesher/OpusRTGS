using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                   // rtgsRead.Run();
                   // rtgsInbound.Run();
                   // bbOutBoundData.Run();

                    rtgsStatusUpdate.Run();

                    Console.WriteLine(".....DONE......");
                    Console.WriteLine("-------------------------------------------------------------------------------\n");
                    Console.WriteLine("Waiting...........");

                    System.Threading.Thread.Sleep(3 * 60 * 1000);
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
        private readonly string bat;
        private ProcessStartInfo psi;
        private Process process;
        public RTGSRead()
        {
            try
            {
                bat = @"D:\OpusBatch\RTGS_Read.bat";
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

    public class RTGSInbound
    {
        private readonly string bat;
        private ProcessStartInfo psi;
        private Process process;
        public RTGSInbound()
        {
            try
            {
                bat = @"D:\OpusBatch\RTGSInbound.bat";
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
                Console.WriteLine("Error For RTGS Inbound Constructor.");
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


    public class BBOutBoundData
    {
        private readonly string bat;
        private ProcessStartInfo psi;
        private Process process;
        public BBOutBoundData()
        {
            try
            {
                bat = @"D:\OpusBatch\RTGSBBOutBoundData.bat";
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

    public class RTGSReturn
    {
        private readonly string bat;
        private ProcessStartInfo psi;
        private Process process;
        public RTGSReturn()
        {
            try
            {
                bat = @"D:\OpusBatch\RTGS_Return.bat";
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
}
