using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FTPclient
{
    public partial class Form1 : Form
    {
        private Socket socket;
        private string messageRecord = "";
        private string path = "";

        public Form1()
        {
            InitializeComponent();
        }


        #region 通用函数
        //接收报文
        private string getReply()
        {
            string receivedString = "";
            //用于接受报文的缓冲区
            byte[] receivedBytes = new byte[1024];
            try
            {
                int bytelength = socket.Receive(receivedBytes, receivedBytes.Length, 0);
                //将字节数组转化为字符串
                receivedString += Encoding.ASCII.GetString(receivedBytes, 0, bytelength);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            messageRecord += ("sever: " + receivedString);
            richTextBox1.Text = messageRecord;
            return receivedString;
        }
        //发送报文
        private void sendCommand(string theCommend)
        {
            try
            {
                byte[] theSendCommand = Encoding.ASCII.GetBytes(theCommend + "\r\n");
                socket.Send(theSendCommand, theSendCommand.Length, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            messageRecord += ("client: " + theCommend + "\t\n");
            richTextBox1.Text = messageRecord;
        }

        //显示目录
        private void showList(string path)
        {
            //清空之前显示的内容
            listBox1.Items.Clear();
            //被动方式建立TCP连接
            string iptext = this.txt_IP.Text.ToString().Trim();
            int dataport = getDataPort();//获取数据端口
            IPAddress ip = IPAddress.Parse(iptext);
            IPEndPoint dataEnd = new IPEndPoint(ip, dataport);
            Socket dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            dataSocket.Connect(dataEnd);
            //发送LIST命令
            string showListCommand = "List " + path + "\r\n";
            sendCommand(showListCommand);
            //接受到返回的列表信息
            string hh = getReply();


            string replyString = "";
            byte[] replyBytes = new byte[1024];
            int replyBytesLength = dataSocket.Receive(replyBytes, replyBytes.Length, 0);
            //转化为字符串
            replyString += Encoding.UTF8.GetString(replyBytes, 0, replyBytesLength);//用UTF8 防止中文乱码

            messageRecord += "server dataport: " + replyString;
            richTextBox1.Text = messageRecord;
            //用'r'来分裂成不同的文件名
            string[] dirList = replyString.Split('\r');
            foreach (string item in dirList)
            {
                //使用正则表达式出去掉其中的空格
                string item1 = Regex.Replace(item, @".*\s{3,12}", "");
                listBox1.Items.Add(item1);
            }

            dataSocket.Close();
            //getReply();
            this.lab_local.Text = path;
        }

        //获取数据端口(被动)
        public int getDataPort()
        {
            //发送被动连接命令
            string sendPASV = "PASV";
            sendCommand(sendPASV);

            try
            {
                //获得服务器应答，其中包括数据连接的端口号
                string pasvReply = getReply();
                //对字符串进行处理获取端口号
                string[] tmp = pasvReply.Split(',');

                int p1 = Convert.ToInt32(tmp[4]);

                string p2String = tmp[5];
                //使用正则表达式获取其中的数
                Regex re = new Regex(@"\d+");
                Match match = re.Match(p2String);
                string newp2String = match.Groups[0].Value;
                int p2 = Convert.ToInt32(newp2String);

                int dataPort = p1 * 256 + p2;
                return dataPort;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        //改变路径
        private void changePath(string path)
        {
            string sendCWD = "CWD " + path;
            sendCommand(sendCWD);
            string replyString = getReply();
            MessageBox.Show(replyString);
            if (replyString.Contains("550"))
            {
                MessageBox.Show("所选文件不是文件夹，无法打开查看");
                return;
            }
            Regex re = new Regex("\"([^\"]*)\"");//使用正则表达式提取当前路径
            Match match = re.Match(replyString);
            string newpath = match.Groups[0].Value;
            string newpath1 = newpath.Remove(0, 1);
            string newpath2 = newpath1.Remove(newpath1.Length - 1, 1);
            showList(newpath2);
            this.path = newpath2;
            lab_local.Text = this.path;
        }
        #endregion

        
        private void Btn_Connect_Click(object sender, EventArgs e)
        {
            //连接服务器部分
            string iptxt = this.txt_IP.Text.ToString().Trim();
            int port = 21;

            IPAddress ip = IPAddress.Parse(iptxt);
            IPEndPoint ipp = new IPEndPoint(ip, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(ipp);
            }
            catch
            {
                MessageBox.Show("连接服务器失败！");
                return;
            }
            string receiveStr = getReply();
            if (receiveStr.Substring(0, 3) == "220")
            {
                MessageBox.Show("已成功连接到" + iptxt + "ftp服务器！");
            }
            else
            {
                MessageBox.Show("连接失败！");
                return;
            }

            //验证登陆
            string nametxt = this.txt_Name.Text.ToString().Trim();
            string passwordtxt = this.txt_Password.Text.ToString().Trim();

            string sendUsername = "User " + nametxt;
            sendCommand(sendUsername);
            string replyString = getReply();
            if (replyString.Substring(0, 3) != "331")
            {
                MessageBox.Show("登录名错误");
                return;
            }

            string sendPassword = "Pass " + passwordtxt;
            sendCommand(sendPassword);
            replyString = getReply();

            if (replyString.Substring(0, 3) == "230")
            {
                MessageBox.Show("登录成功！");
            }
            else
            {
                MessageBox.Show("密码错误");
                return;
            }
            this.path = "/";
            showList(this.path);

        }//连接

        public string FolderName = "";
        private void Btn_new_Click(object sender, EventArgs e)
        {
            this.FolderName = "";
            Form2 login = new Form2(this);
            login.ShowDialog();
            if (this.FolderName!="")
            {
                string sendMKD = "MKD " + this.FolderName;
                sendCommand(sendMKD);
                string mkdReply = getReply();
                if (mkdReply.Contains("already"))
                {
                    MessageBox.Show("文件夹" + this.FolderName + "不存在");
                }

                this.FolderName = "";
                MessageBox.Show("新建文件夹成功");
                showList(this.path);
            }
        }//新建文件夹

        private void Btn_Open_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择打开的文件夹！");
                return;
            }

            try
            {
                string selectString = listBox1.SelectedItem.ToString();
                string[] stringArray = selectString.Split(' ');
                string selectedFile = stringArray[4];
                changePath(selectedFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }//进入文件夹

        private void Btn_Quit_Click(object sender, EventArgs e)
        {
            string[] stringArray = this.path.Split(Convert.ToChar(47));
            string newpath = "";
            if (stringArray.Length == 2)//   "/"
            {
                if (stringArray[1] == "") return;
                else newpath = "/";
            }
            else
            {
                for (int i = 1; i < stringArray.Length - 1; i++)
                {
                    newpath += ("/" + stringArray[i]);
                }
            }
            changePath(newpath);
        }//返回上级目录

        private void Btn_upload_Click(object sender, EventArgs e)
        {
            string upFileName="";
            string upFilePath="";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "请选择上传文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                upFileName = ofd.FileName;
                string[] upFileNameArray = upFileName.Split(Convert.ToChar(92));  // \
                upFileName = upFileNameArray[upFileNameArray.Length - 1];

                //计算路径
                string sb = "";
                for (int i = 0; i <= upFileNameArray.Length - 2; i++)
                {
                    sb += upFileNameArray[i];
                    if (i != upFileNameArray.Length - 2)
                    {
                        sb += @"\";
                    }
                }
                upFilePath = sb;
            }//获取文件路径和名称（分开）
            else
            {
                return;
            }
            upLoad(upFileName, upFilePath);
            string replyString = getReply();
            MessageBox.Show(replyString);
            showList(this.path);
            //MessageBox.Show("文件上传成功");
        }//上传按钮

        private void upLoad(string fileName, string clientPath)
        {
            

            //开启数据socket
            int dataPort = getDataPort();
            IPAddress ip = IPAddress.Parse(this.txt_IP.Text);
            IPEndPoint ipp = new IPEndPoint(ip, dataPort);

            Socket dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            dataSocket.Connect(ipp);

            FileInfo theFileInfo = new FileInfo(clientPath + @"\" + fileName);
            int fileSize = (int)theFileInfo.Length;
            int currentSize = 0;
            if (File.Exists(clientPath + fileName) == true)//如果本地有文件，用REST偏移
            {
                string sendFileSize = "SIZE " + fileName + "\r\n";
                sendCommand(sendFileSize);
                string sizeReply = getReply();
                string[] size = sizeReply.Split(' ');
                currentSize = int.Parse(size[1]);

                string sendRest = "REST " + fileName + "\r\n";
                sendCommand(sendRest);
                string sizeReply2 = getReply();
            }


            string sendSTOR = "STOR " + fileName;
            sendCommand(sendSTOR);

            string storString = getReply();
            if (storString.Contains("550"))
            {
                MessageBox.Show("服务器目录已存在该文件名");
                return;
            }

            //设立缓冲区
            byte[] sendByte = new byte[fileSize-currentSize];
            int sendByteLength;
            using (FileStream fs = new FileStream(clientPath + @"\" + fileName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                int sumLength = 0;
                while (sumLength < sendByte.Length)
                {
                    fs.Read(sendByte, 0, sendByte.Length);
                    sendByteLength = dataSocket.Send(sendByte, sendByte.Length, 0);
                    sumLength += sendByteLength;
                }
            }
            dataSocket.Close();
        }//上传实现

        private void Btn_download_Click(object sender, EventArgs e)
        {
            if (this.listBox1.SelectedItem == null)
            {
                MessageBox.Show("请先选择下载的文件");
                return;
            }

            string selectedFileName = listBox1.SelectedItem.ToString().Split(' ')[4];//要下载的文件名
            string clientPath;//下载文件路径

            //打开文件选择对话框，选择保存路径
            FolderBrowserDialog ofd = new FolderBrowserDialog();
            ofd.Description = "请先选择保存路径";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                clientPath = ofd.SelectedPath;
            }
            else
            {
                return;
            }
            DownLoad(selectedFileName, clientPath);

            MessageBox.Show("文件传输完毕！");
        }//下载按钮

        private void DownLoad(string fileName, string clientPath)
        {
            int fileSize = getSize(fileName);

           
            string sendFileSize = "SIZE " + fileName + "\r\n";
            sendCommand(sendFileSize);
            string sizeReply = getReply();

            //如果是文件，下载文件
            if (sizeReply.Contains("213"))//213 <size>
            {
                int dataPort = getDataPort();
                IPAddress ip = IPAddress.Parse(this.txt_IP.Text);
                IPEndPoint ipp = new IPEndPoint(ip, dataPort);
                Socket dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                dataSocket.Connect(ipp);

                if (File.Exists(fileName) == true)//如果本地有文件，用REST偏移
                {
                    FileInfo fileInfo = new FileInfo(fileName);
                    double length = Convert.ToDouble(fileInfo.Length);
                    double Size = length;
                    string send1 = "REST " + Size + "\r\n";
                    sendCommand(send1);
                    string ret = getReply();
                }

                //发送retr命令
                string sendRETR = "RETR " + fileName + "\r\n";
                sendCommand(sendRETR);
                string retrReply = getReply();

                if (retrReply.Contains("550"))
                {
                    MessageBox.Show("文件" + fileName + "不存在");
                    return;
                }

                //计算保存路径
                string savePath = clientPath + @"\" + fileName;

                byte[] recvByte = new byte[fileSize];
                int recvByteLength;
                //为该文件产生流，socket向缓冲区中写信息，流从缓冲区中取出信息写入文件
                using (FileStream fs = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    int sumLength = 0;
                    //循环继续的条件是当前接收到的字节数还少于改文件总字节数
                    while (sumLength < recvByte.Length)
                    {
                        //从数据TCP连接中获取数据
                        recvByteLength = dataSocket.Receive(recvByte, recvByte.Length, 0);
                        fs.Write(recvByte, 0, recvByteLength);
                        //每次获得信息字节后都要对接受到的字节累加
                        sumLength += recvByteLength;
                        //totalLength += recvByteLength;
                    }
                }
                //关闭数据TCP连接
                dataSocket.Close();
            }
           

        }

        private int getSize(string fileName)
        {
            string sendFileSize = "SIZE " + fileName;
            sendCommand(sendFileSize);
            string sizeReply = getReply();

            //如果是文件
            if (sizeReply.Contains("213"))
            {
                if (sizeReply.Contains("226"))
                {
                    string[] newSizeReply = sizeReply.Split('\r');
                    string[] newSizeReply1 = newSizeReply[1].Split(Convert.ToChar(' '));
                    string newp2String = newSizeReply1[1];
                    return Convert.ToInt32(newp2String);
                }
                else
                {
                    string[] newSizeReply = sizeReply.Split('\r');
                    string[] newSizeReply1 = newSizeReply[0].Split(Convert.ToChar(' '));
                    string newp2String = newSizeReply1[1];
                    return Convert.ToInt32(newp2String);
                }
            }
            //如果是文件夹
            else if (sizeReply.Contains("550"))
            {
                int sum = 0;
                changePath(fileName);
                string iptext = this.txt_IP.Text.ToString().Trim();
                int dataport = getDataPort();
                IPAddress ip = IPAddress.Parse(iptext);
                IPEndPoint dataEnd = new IPEndPoint(ip, dataport);

                Socket dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                dataSocket.Connect(dataEnd);

                string showListCommand = "List " + this.path + "\r\n";
                sendCommand(showListCommand);

                string hh = getReply();
                //string[] stringArray = hh.Split(' ');


                string replyString = "";
                byte[] replyBytes = new byte[1024];
                int replyBytesLength = dataSocket.Receive(replyBytes, replyBytes.Length, 0);
                replyString += Encoding.ASCII.GetString(replyBytes, 0, replyBytesLength);

                if (replyString == "")
                {
                    Btn_Quit_Click(new object(), new EventArgs());
                    return 0;
                }

                string[] dirList = replyString.Split('\r');
                foreach (string item in dirList)
                {
                    if (item != "\n")
                    {
                        string[] stringArray = item.Split(' ');
                        sum += getSize(stringArray[stringArray.Length - 1]);//递归调用
                    }
                }
                Btn_Quit_Click(new object(), new EventArgs());
                return sum;
            }
            else
            {
                return -1;
            }

        }
    }
}
