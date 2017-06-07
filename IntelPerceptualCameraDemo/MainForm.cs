using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace IntelPerceptualCameraDemo
{
    public partial class MainForm : Form
    {
        private PXCMSession session;
        private volatile bool closing = false;
        private volatile bool stop = false;
        private Bitmap[] bitmaps = new Bitmap[2];

        ArrayList devices = new ArrayList();
        ArrayList devices_iuid = new ArrayList();
        ArrayList profiles = new ArrayList();
        ArrayList profilesDepth = new ArrayList();

        const string LogitechCameraName = "Logitech HD Webcam C270";
        const string IntelCameraName = "";


        public MainForm(PXCMSession session)
        {
            InitializeComponent();

            this.session = session;

            FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
            MainPanel.Paint += new PaintEventHandler(Panel_Paint);
            PIPPanel.Paint += new PaintEventHandler(Panel_Paint);

            GetDeviceInfo();
            Depth.Checked = true;
        }

        public void GetDeviceInfo()
        {
            devices.Clear();
            devices_iuid.Clear();

            PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();  //建立module implementation
            desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;

            for (uint i = 0; ; i++)  //枚举视频模块
            {
                PXCMSession.ImplDesc desc1;
                if (session.QueryImpl(ref desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;   //根据描述符模块Template——desc枚举匹配的模块desc1
                PXCMCapture capture;
                if (session.CreateImpl<PXCMCapture>(ref desc1, PXCMCapture.CUID, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;  //创建视频模块实例
                for (uint j = 0; ; j++)  //枚举设备
                {
                    PXCMCapture.DeviceInfo dinfo;
                    if (capture.QueryDevice(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;  //对于视频模块，查找器件信息

                    string dinfoString = dinfo.name.get();
                    devices.Add(dinfo);
                    devices_iuid.Add(desc1.iuid);
                }
                capture.Dispose();
            }

            GetColorDepthInfo(devices_iuid);
        }

        private void GetColorDepthInfo(ArrayList devices_iuid)
        {
            PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
            desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;
            desc.iuid = (int)devices_iuid[0];  //
            desc.cuids[0] = PXCMCapture.CUID;

            profiles.Clear();

            PXCMCapture capture;
            if (session.CreateImpl<PXCMCapture>(ref desc, PXCMCapture.CUID, out capture) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                PXCMCapture.Device device;
                if (capture.CreateDevice(GetCheckedDevice().didx, out device) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                {   //创建设备
                    bool cpopulated = false, dpopulated = false;
                    for (uint s = 0; ; s++)  //枚举流
                    {
                        PXCMCapture.Device.StreamInfo sinfo;
                        if (device.QueryStream(s, out sinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                        if (sinfo.cuid != PXCMCapture.VideoStream.CUID) continue;

                        if (((int)sinfo.imageType & (int)PXCMImage.ImageType.IMAGE_TYPE_COLOR) != 0 && !cpopulated)  //彩色图像流
                        {
                            PXCMCapture.VideoStream stream;
                            if (device.CreateStream<PXCMCapture.VideoStream>(s, PXCMCapture.VideoStream.CUID, out stream) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                            {
                                for (uint p = 0; ; p++)
                                {
                                    PXCMCapture.VideoStream.ProfileInfo pinfo;
                                    if (stream.QueryProfile(p, out pinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                                    profiles.Add(pinfo);  //**
                                    cpopulated = true;
                                }
                                stream.Dispose();
                            }
                        }
                        if (((int)sinfo.imageType & (int)PXCMImage.ImageType.IMAGE_TYPE_DEPTH) != 0 && !dpopulated)  //深度图像流
                        {
                            PXCMCapture.VideoStream stream;
                            if (device.CreateStream<PXCMCapture.VideoStream>(s, PXCMCapture.VideoStream.CUID, out stream) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                            {
                                for (uint p = 0; ; p++)
                                {
                                    PXCMCapture.VideoStream.ProfileInfo pinfo;
                                    if (stream.QueryProfile(p, out pinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                                    profilesDepth.Add(pinfo); //**
                                    dpopulated = true;
                                }
                                stream.Dispose();
                            }
                        }
                    }
                    device.Dispose();
                }
                capture.Dispose();
            }
        }

        public int count = 0;
        public void SetBitmap(int index, int width, int height, byte[] pixels)
        {
            lock (this)
            {
                if (bitmaps[index] != null) bitmaps[index].Dispose();
                bitmaps[index] = new Bitmap(width, height, PixelFormat.Format32bppRgb);
                BitmapData data = bitmaps[index].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                Marshal.Copy(pixels, 0, data.Scan0, width * height * 4);            
                bitmaps[index].UnlockBits(data);
                //if (count <= 19)
                //{
                //    if (index == 0)
                //    {
                //        string bmpSavedFilePath = rs.bmpSavedFileFolderPath + "\\" + count.ToString() + ".bmp";
                //        bitmaps[index].Save(bmpSavedFilePath, System.Drawing.Imaging.ImageFormat.Bmp);  
                //        count++;
                //    }
                //}
            }
        }

        public bool GetDepthRawState()
        {
            return DepthRaw.Checked;
        }

        public bool GetDepthState()
        {
            return Depth.Checked;
        }

        public bool GetStopState()
        {
            return stop;
        }

        public PXCMCapture.DeviceInfo GetCheckedDevice()
        {
            if (devices.Count == 0) return new PXCMCapture.DeviceInfo();
            return (PXCMCapture.DeviceInfo)devices[0];
        }

        public PXCMCapture.VideoStream.ProfileInfo GetColorConfiguration()
        {
            if (profiles.Count == 0) return new PXCMCapture.VideoStream.ProfileInfo();
            return (PXCMCapture.VideoStream.ProfileInfo)profiles[0];
        }

        public PXCMCapture.VideoStream.ProfileInfo GetDepthConfiguration()
        {
            if (profilesDepth.Count == 0) return new PXCMCapture.VideoStream.ProfileInfo();
            return (PXCMCapture.VideoStream.ProfileInfo)profilesDepth[0];
        }

        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            lock (this)
            {
                Bitmap bitmap = bitmaps[(sender == MainPanel) ? 0 : 1];  //第一幅图是MainPanel显示，第二幅图是PIPPanel显示
                if (bitmap == null) return;
                //bitmap.Save(rs.bmpSavedFileFolderPath, System.Drawing.Imaging.ImageFormat.Bmp);  //**错误，红叉，保存路径不对没有加文件名

                /* Keep the aspect ratio */
                Rectangle rc = (sender as PictureBox).ClientRectangle;
                float xscale = (float)rc.Width / (float)bitmap.Width;
                float yscale = (float)rc.Height / (float)bitmap.Height;
                float xyscale = (xscale < yscale) ? xscale : yscale;
                int width = (int)(bitmap.Width * xyscale);
                int height = (int)(bitmap.Height * xyscale);
                rc.X = (rc.Width - width) / 2;
                rc.Y = (rc.Height - height) / 2;
                rc.Width = width;
                rc.Height = height;
                e.Graphics.DrawImage(bitmap, rc);
            }
        }

        public void SaveBitmap(int index)
        {
            lock (this)
            {
                if (index == 0)
                {
                    //Bitmap savedBitmap = new Bitmap(bitmaps[0]);
                    Bitmap savedBitmap = new Bitmap(bitmaps[0].Width, bitmaps[0].Height);
                    //将第一个bmp拷贝到bmp2中                  
                    Graphics draw = Graphics.FromImage(savedBitmap);
                    draw.DrawImage(bitmaps[0], bitmaps[0].Width, bitmaps[0].Height);  
                    //路径
                    savedBitmap.Save(rs.bmpSavedFileFolderPath, System.Drawing.Imaging.ImageFormat.Bmp);
                    draw.Dispose();
                }
            }
        }

        private delegate void UpdatePanelDelegate();
        public void UpdatePanel()  //刷新MainPanel和PIPPanel
        {
            MainPanel.Invoke(new UpdatePanelDelegate(delegate()
            { MainPanel.Invalidate(); PIPPanel.Invalidate(); }));
        }

        private delegate void UpdateStatusDelegate(string status);
        public void UpdateStatus(string status)  //更新状态条
        {
            Status2.Invoke(new UpdateStatusDelegate(delegate(string s) { StatusLabel.Text = s; }), new object[] { status });
        }

        private void Start_Click(object sender, EventArgs e)
        {
            //stop = false;
            //string deviceName = ((PXCMCapture.DeviceInfo)devices[0]).name.get();
            //string deviceName2 = ((PXCMCapture.DeviceInfo)devices[1]).name.get();
            //Thread thread = null, thread2 = null;
            //if (deviceName == LogitechCameraName && deviceName2 == LogitechCameraName)
            //{
            //    thread = new ParameterizedThreadStart(new Thread(DoRenderingAsync));
            //    thread.Start();
            //    System.Threading.Thread.Sleep(5);

            //    thread2 = new Thread(DoRenderingAsync);
            //    thread2.Start();
            //}
            //else if (deviceName==IntelCameraName || deviceName2==IntelCameraName)
            //{
            //    thread = new System.Threading.Thread(DoRenderingSync);
            //    thread.Start();
            //    System.Threading.Thread.Sleep(5);

            //    thread2 = new Thread(DoRenderingAsync);
            //    thread2.Start();
            //}

            stop = false;
            System.Threading.Thread thread = new System.Threading.Thread(DoRendering);
            thread.Start();
            System.Threading.Thread.Sleep(5);

        }

        delegate void DoRenderingBegin();
        delegate void DoRenderingEnd();
        RenderStreams rs;
        private void DoRendering()   //真正显示视频和深度数据线程
        {
            rs = new RenderStreams(this);

            Invoke(new DoRenderingBegin(
                delegate
                {
                    Start.Enabled = false;
                    Stop.Enabled = true;
                }
            ));
            
            //rs.RunColorDepthSync();
            rs.RunColorDepthAsync();
          
            this.Invoke(new DoRenderingEnd(
                delegate
                {
                    Start.Enabled = true;
                    Stop.Enabled = false;
                    if (closing) Close();
                }
            ));
        }

        private void DoRenderingAsync()   //真正显示视频和深度数据线程
        {
            RenderStreams rs = new RenderStreams(this);

            Invoke(new DoRenderingBegin(
                delegate
                {
                    Start.Enabled = false;
                    Stop.Enabled = true;
                }
            ));

            rs.RunColorDepthAsync();

            this.Invoke(new DoRenderingEnd(
                delegate
                {
                    Start.Enabled = true;
                    Stop.Enabled = false;
                    if (closing) Close();
                }
            ));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            stop = true;
            e.Cancel = Stop.Enabled;  //Stop使能的情况下，窗口不能关闭
            closing = true;
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            stop = true;
        }
    }
}
