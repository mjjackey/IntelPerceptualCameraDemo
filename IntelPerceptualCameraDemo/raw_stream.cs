using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace IntelPerceptualCameraDemo
{
    struct PerceptualCamera3DimensionCoord
    {
        public int x;
        public int y;
        public int z;
        public int col;
        public int row;
    }

    class RenderStreams
    {
        private MainForm form;
        internal string bmpSavedFileFolderPath = "";
        internal int calibrationOrActualFlag = 2;  //校正或实际测量的标志：0~普通状态，1~测量，2~校正
        internal int yp0 = 0;  //磁球负限位时在相机坐标系中的坐标
        int xp0 = 0, zp0 = 0;
        //int saveImageCount = 0;
        uint depthWidth = 0;
        uint depthHeight = 0;
        const int FilterDistanceConstant = 30;
        PerceptualCamera3DimensionCoord ThereDCoord;
        List<PerceptualCamera3DimensionCoord> ThereDCoordList = new List<PerceptualCamera3DimensionCoord>();
        List<PerceptualCamera3DimensionCoord> ThereDCoordListDesc = new List<PerceptualCamera3DimensionCoord>();
        IEnumerable<PerceptualCamera3DimensionCoord> ThereDCoordQuery = null;
        public ManualResetEvent depthToRealWordEvent = new ManualResetEvent(false);


        public RenderStreams(MainForm mf)
        {
            form = mf;
            bmpSavedFileFolderPath = Application.StartupPath + "\\Capture Image";
            if (!File.Exists(bmpSavedFileFolderPath))
            {
                Directory.CreateDirectory(bmpSavedFileFolderPath);
            }
        }

        public static int ALIGN16(uint width)
        {
            return ((int)((width + 15) / 16)) * 16;
        }

        public static byte[] GetRGB32Pixels(PXCMImage image)
        {
            int cwidth = ALIGN16(image.info.width); /* aligned width */
            int cheight = (int)image.info.height;

            PXCMImage.ImageData cdata;
            byte[] cpixels;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_RGB32, out cdata)>=pxcmStatus.PXCM_STATUS_NO_ERROR) 
            {
                cpixels = cdata.ToByteArray(0, cwidth * cheight * 4);
                image.ReleaseAccess(ref cdata);
            }
            else
            {
                cpixels = new byte[cwidth * cheight * 4];
            }
            return cpixels;
        }

        /// <summary>
        /// 如果只有彩色图像信息的摄像头只能用异步方式，第一次pp.AcquireFrame(true)为true，第二次就为false
        /// </summary>
        public void RunColorDepthSync() /* Stream Color and Depth synchronously */
        {
            bool sts = true;

            /* UtilMPipeline works best for synchronous color and depth streaming */
            UtilMPipeline pp = new UtilMPipeline();
               
            /* Set Input Source */
            PXCMCapture.DeviceInfo dinfo2 = form.GetCheckedDevice();  //要改
            pp.capture.SetFilter(ref dinfo2);

            /* Set Color & Depth Resolution */
            PXCMCapture.VideoStream.ProfileInfo cinfo = form.GetColorConfiguration();
            pp.EnableImage(cinfo.imageInfo.format, cinfo.imageInfo.width, cinfo.imageInfo.height);  //select the color stream
            pp.capture.SetFilter(ref cinfo); // only needed to set FPS

            PXCMCapture.VideoStream.ProfileInfo dinfo = form.GetDepthConfiguration();
            pp.EnableImage(dinfo.imageInfo.format, dinfo.imageInfo.width, dinfo.imageInfo.height);
            pp.capture.SetFilter(ref dinfo); // only needed to set FPS

            /* Initialization */
            FPSTimer timer = new FPSTimer(form);
            form.UpdateStatus("Init Started");
            if (pp.Init())
            {
                /* For UV Mapping & Projection only: Save certain properties */
                Projection projection = new Projection(pp.session, pp.capture.device,this);

                form.UpdateStatus("Streaming");
                while (!form.GetStopState())
                {
                    /* If raw depth is needed, disable smoothing */
                    pp.capture.device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SMOOTHING, form.GetDepthRawState() ? 0 : 1);  //**区分Depth和Depth Raw

                    /* Wait until a frame is ready */
                    if (!pp.AcquireFrame(true)) break;
                    if (pp.IsDisconnected()) break;

                    /* Display images */
                    PXCMImage color = pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_COLOR);  //retrieve the color sample
                    PXCMImage depth = pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_DEPTH);  //retrieve the depth sample

                    form.SetBitmap(0, ALIGN16(color.info.width), (int)color.info.height, GetRGB32Pixels(color));  //得到彩色图像
                    //form.SaveBitmap(0);
                    form.SetBitmap(1, ALIGN16(depth.info.width), (int)depth.info.height, GetRGB32Pixels(depth));  //得到深度图像  //要改
                    timer.Tick(color.info.format.ToString().Substring(13) + " " + color.info.width + "x" + color.info.height + " "+ depth.info.format.ToString().Substring(13) + " " + depth.info.width + "x" + depth.info.height);
                    
                    form.UpdatePanel();

                    //saveImageCount++;  //**MJ
                    PXCMPoint3DF32[] realCords = projection.DepthToRealWord(depth);
                    SaveRealCordsToFile(realCords);

                    pp.ReleaseFrame();  //释放流，go fetching the next samples
                }
                projection.Dispose();
            }
            else
            {
                form.UpdateStatus("Init Failed");
                sts = false;
            }

            pp.Close();   // close down
            pp.Dispose();
            if (sts) form.UpdateStatus("Stopped");
        }

        public void RunColorDepthAsync() /* Stream color and depth independently */
        {
            bool sts = true;

            PXCMSession session;
            pxcmStatus sts2 = PXCMSession.CreateInstance(out session);
            if (sts2 < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                form.UpdateStatus("Failed to create an SDK session");
                return;
            }

            /* UtilMCapture works best for asychronous color and depth streaming */
            UtilMCapture uc = new UtilMCapture(session);
               
            /* Set Inpt Source */
            PXCMCapture.DeviceInfo dinfo2 = form.GetCheckedDevice();  //要改
            uc.SetFilter(ref dinfo2);

            /* Set Color & Depth Resolution */
            int nstreams = 0;   //计数器，计数彩色图像和深度图像流个数
            PXCMCapture.VideoStream.DataDesc desc = new PXCMCapture.VideoStream.DataDesc();
            PXCMCapture.VideoStream.DataDesc.StreamDesc sdesc = new PXCMCapture.VideoStream.DataDesc.StreamDesc();

            PXCMCapture.VideoStream.ProfileInfo cinfo = form.GetColorConfiguration();  //要改
            if (cinfo.imageInfo.format != 0)
            {
                uc.SetFilter(ref cinfo); // only needed to set FPS
                sdesc.format = cinfo.imageInfo.format;
                sdesc.sizeMin.width = cinfo.imageInfo.width;
                sdesc.sizeMin.height = cinfo.imageInfo.height;
                desc.streams[nstreams++] = sdesc;
                
            }

            PXCMCapture.VideoStream.ProfileInfo dinfo = form.GetDepthConfiguration();  //要改
            if (dinfo.imageInfo.format != 0)
            {
                uc.SetFilter(ref dinfo); // only needed to set FPS
                sdesc.format = dinfo.imageInfo.format;
                sdesc.sizeMin.width = dinfo.imageInfo.width;
                sdesc.sizeMin.height = dinfo.imageInfo.height;
                desc.streams[nstreams++] = sdesc;
                depthWidth = dinfo.imageInfo.width;  //**MJ
                depthHeight = dinfo.imageInfo.height;
            }

            /* Initialization */
            form.UpdateStatus("Init Started");
            if (uc.LocateStreams(ref desc) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                PXCMImage[] images = new PXCMImage[nstreams];
                PXCMScheduler.SyncPoint[] sps = new PXCMScheduler.SyncPoint[nstreams];
                int[] panels = new int[2] { 0, 1 };

                Projection projection = new Projection(session, uc.device,this);  //**MJ

                /* initialize first read */
                for (int i = 0; i < nstreams; i++)
                    sts2 = uc.QueryVideoStream(i).ReadStreamAsync(out images[i], out sps[i]);

                form.UpdateStatus("Streaming");
                FPSTimer timer = new FPSTimer(form);
                while (!form.GetStopState())
                {
                    uint idx;
                    PXCMScheduler.SyncPoint.SynchronizeEx(sps, out idx); /* wait until a sample is ready */

                    /* If raw depth is needed, disable smoothing */
                    uc.device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SMOOTHING, form.GetDepthRawState() ? 0 : 1);

                    /* Set main panel and PIP panel index */
                    panels[0] = ((form.GetDepthState() || form.GetDepthRawState()) && nstreams > 1) ? 0 : 1;  //去掉  //**MJ //如果选择深度图像选项按钮，则panels[0]值为0，PIP panel显示深度图像
                    panels[1] = 1 - panels[0];

                    //saveImageCount++;
                    for (int i = (int)idx; i < nstreams; i++) /* loop through all streams for all available streams */
                    {
                        sts2 = sps[i].Synchronize(0);
                        if (sts2 == pxcmStatus.PXCM_STATUS_EXEC_INPROGRESS) continue;
                        if (sts2 < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                        /* initiate next read */
                        PXCMImage picture = images[i];
                        images[i] = null;   //**
                        sps[i].Dispose();
                        sts2 = uc.QueryVideoStream(i).ReadStreamAsync(out images[i], out sps[i]);
                       
                        if (images[i].info.format == PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH) //**MJ  //如果是深度图像
                        {                         
                            PXCMPoint3DF32[] realCords = projection.DepthToRealWord(images[i]);
                            //SaveRealCordsToFile(realCords);

                            //Thread.Sleep(50);
                            depthToRealWordEvent.WaitOne(50);
                            GetDistance(realCords);
                            depthToRealWordEvent.Reset();
                        }
                        //if (desc.streams[i].format == PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH)  //也可以
                        //{

                        //}

                        /* Display only the selected picture */
                        form.SetBitmap(panels[i], ALIGN16(desc.streams[i].sizeMin.width), (int)desc.streams[i].sizeMin.height, GetRGB32Pixels(picture)); //i=0是彩色图像  //要改
                        if (panels[i] == 0) timer.Tick(picture.info.format.ToString().Substring(13) + " " + desc.streams[i].sizeMin.width + "x" + desc.streams[i].sizeMin.height);
                        picture.Dispose();

                        if (sts2 < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                    }
                    if (sts2 == pxcmStatus.PXCM_STATUS_EXEC_INPROGRESS) continue;
                    if (sts2 < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                    form.UpdatePanel();
                }
                PXCMScheduler.SyncPoint.SynchronizeEx(sps);
                PXCMImage.Dispose(images);
                PXCMScheduler.SyncPoint.Dispose(sps);
            }
            else
            {
                form.UpdateStatus("Init Failed");
                sts = false;
            }

            uc.Dispose();
            session.Dispose();
            if (sts) form.UpdateStatus("Stopped");
        }

        private void SaveRealCordsToFile(PXCMPoint3DF32[] realCords)
        {
            //if (++saveImageCount % 20 == 0)
            lock (form)
            {
                if (form.count <= 19)
                { 
                    //if (saveImageCount <= 19)
                    //{
                    //string distanceFilePath = bmpSavedFileFolderPath + "\\" + saveImageCount.ToString() + ".txt";  //每一帧图像新建一个文件
                    string distanceFilePath = bmpSavedFileFolderPath + "\\" + form.count.ToString() + ".txt";
                    FileStream distanceFileStream = File.Open(distanceFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    StreamWriter distanceStreamWriter = new StreamWriter(distanceFileStream);
                    int i = 0;
                    for (i = 0; i < realCords.Length; i++)
                    {
                        distanceStreamWriter.WriteLine(realCords[i].x + "," + realCords[i].y + "," + realCords[i].z);
                    }
                    distanceStreamWriter.Flush();
                    distanceStreamWriter.Close();
                    distanceFileStream.Close();
                    //}
                    form.count++;
                }         
            }
        }

        internal int distance = 30;  //物体最高点与磁球负限位的距离，变值，为所求值
        //const int colDown = 130, colUp = 190, rowDown = 60, rowUp = 120;
        const int colDown = 100, colUp = 220, rowDown = 60, rowUp = 120;
        void GetDistance(PXCMPoint3DF32[] realCords)
        {
            int x = 0, y = 0, z = 0;
            int maxy = -1000;
            int maxIndex = 0;
            int yp0Index = 0;

            int xl = 200;   //以mm为单位，床的长度
            int xr = -xl;
            int zn = 100;    //Z方向最近距离（需要测量）
            //int zf = 1000;
            int zf = 900;
            //int yd = -300;
            int yd = -200;

            int row = 0;
            int col = 0;

            ThereDCoordList.Clear();
            ThereDCoordListDesc.Clear();
            int i = 0;
            //for (i = 0; i < realCords.Length; i++)  //也可以
            //{
            //    row = (int)(i / depthWidth);
            //    col = i - (int)(row * depthWidth);

            //    if (col < colDown || col > colUp || row < rowDown || row > rowUp) continue;

            //    x = (int)(realCords[i].x * 1000); //**要加括号 //单位由m转化为mm
            //    y = (int)(realCords[i].y * 1000);
            //    z = (int)(realCords[i].z * 1000);

            //    //x = realCords[i].x;  
            //    //y = realCords[i].y;
            //    //z = realCords[i].z;

            //    //if (calibrationOrActualFlag == 1)
            //    //{
            //    //    if (x >= xr && x <= xl && z > zn && z < zf && y >= yp0)  //找出低于磁球最低点（磁球不移动时是yp0）的点的坐标
            //    //    {
            //    //        maxy = y;
            //    //        maxIndex = i;
            //    //    }
            //    //}
            //    if (calibrationOrActualFlag == 2)
            //    {
            //        if (x >= xr && x <= xl && z >= zn && z <= zf && y < 0 && y > yd)  //标定磁球负限位时在相机坐标系中的坐标
            //        {
            //            ThereDCoord.x = x;
            //            ThereDCoord.y = y;
            //            ThereDCoord.z = z;
            //            ThereDCoord.row = row;
            //            ThereDCoord.col = col;

            //            ThereDCoordList.Add(ThereDCoord);
            //        }
            //    }
            //}

            int j = 0;
            int realCordsLength = 0;
            if (calibrationOrActualFlag == 2)
            {
                for (i = rowDown; i <= rowUp; i++)  //直接在所在行列区域内搜索，减小循环次数
                {
                    for (j = colDown; j <= colUp; j++)
                    {
                        realCordsLength = Convert.ToInt32(i * depthWidth + j);
                        x = (int)(realCords[realCordsLength].x * 1000); //**要加括号 //单位由m转化为mm
                        y = (int)(realCords[realCordsLength].y * 1000);
                        z = (int)(realCords[realCordsLength].z * 1000);

                        if (calibrationOrActualFlag == 1)
                        {
                            if (x >= xr && x <= xl && z > zn && z < zf && y >= yp0)  //找出低于磁球最低点的点的坐标，求这个区间y>yp0（磁球不移动时是yp0）的y的最大值
                            {
                                maxy = y;  //寻找最大值也要按照下面方法
                                maxIndex = i;
                            }
                        }
                        else if (calibrationOrActualFlag == 2)
                        {
                            if (x >= xr && x <= xl && z >= zn && z <= zf && y < 0 && y > yd)  //标定磁球负限位时在相机坐标系中的坐标，求这个区间y<0的y的最大值
                            {
                                ThereDCoord.x = x;
                                ThereDCoord.y = y;
                                ThereDCoord.z = z;
                                ThereDCoord.row = i;
                                ThereDCoord.col = j;

                                ThereDCoordList.Add(ThereDCoord);
                            }
                        }
                    }
                }

                ThereDCoordQuery = from items in ThereDCoordList orderby items.y descending select items;  //对List中的坐标点按y降序排列
                ThereDCoordListDesc = ThereDCoordQuery.ToList();

                double[] potDistanceArray = new double[6];
                double[] xdisArray = new double[6];
                double[] ydisArray = new double[6];
                double[] zdisArray = new double[6];
                for (i = 0; i < ThereDCoordListDesc.Count; i++)
                {
                    row = ThereDCoordListDesc[i].row;
                    col = ThereDCoordListDesc[i].col;
                    //xdisArray[0] = Math.Pow(realCords[(row - 1) * depthWidth + (col + 1)].x * 1000 - ThereDCoordListDesc[i].x, 2);
                    //ydisArray[0] = Math.Pow(realCords[(row - 1) * depthWidth + (col + 1)].y * 1000 - ThereDCoordListDesc[i].y, 2);
                    //zdisArray[0] = Math.Pow(realCords[(row - 1) * depthWidth + (col + 1)].z * 1000 - ThereDCoordListDesc[i].z, 2);
                    //potDistanceArray[0] = Math.Sqrt(xdisArray[0] + ydisArray[0] + zdisArray[0]);

                    //xdisArray[1] = Math.Pow(realCords[(row - 2) * depthWidth + (col + 2)].x * 1000 - ThereDCoordListDesc[i].x, 2);
                    //ydisArray[1] = Math.Pow(realCords[(row - 2) * depthWidth + (col + 2)].y * 1000 - ThereDCoordListDesc[i].y, 2);
                    //zdisArray[1] = Math.Pow(realCords[(row - 2) * depthWidth + (col + 2)].z * 1000 - ThereDCoordListDesc[i].z, 2);
                    //potDistanceArray[1] = Math.Sqrt(xdisArray[1] + ydisArray[1] + zdisArray[1]);

                    //xdisArray[2] = Math.Pow(realCords[(row - 3) * depthWidth + (col + 3)].x * 1000 - ThereDCoordListDesc[i].x, 2);
                    //ydisArray[2] = Math.Pow(realCords[(row - 3) * depthWidth + (col + 3)].y * 1000 - ThereDCoordListDesc[i].y, 2);
                    //zdisArray[2] = Math.Pow(realCords[(row - 3) * depthWidth + (col + 3)].z * 1000 - ThereDCoordListDesc[i].z, 2);
                    //potDistanceArray[2] = Math.Sqrt(xdisArray[2] + ydisArray[2] + zdisArray[2]);

                    //xdisArray[3] = Math.Pow(realCords[(row - 1) * depthWidth + (col - 1)].x * 1000 - ThereDCoordListDesc[i].x, 2);
                    //ydisArray[3] = Math.Pow(realCords[(row - 1) * depthWidth + (col - 1)].y * 1000 - ThereDCoordListDesc[i].y, 2);
                    //zdisArray[3] = Math.Pow(realCords[(row - 1) * depthWidth + (col - 1)].z * 1000 - ThereDCoordListDesc[i].z, 2);
                    //potDistanceArray[3] = Math.Sqrt(xdisArray[3] + ydisArray[3] + zdisArray[3]);

                    //xdisArray[4] = Math.Pow(realCords[(row - 2) * depthWidth + (col - 2)].x * 1000 - ThereDCoordListDesc[i].x, 2);
                    //ydisArray[4] = Math.Pow(realCords[(row - 2) * depthWidth + (col - 2)].y * 1000 - ThereDCoordListDesc[i].y, 2);
                    //zdisArray[4] = Math.Pow(realCords[(row - 2) * depthWidth + (col - 2)].z * 1000 - ThereDCoordListDesc[i].z, 2);
                    //potDistanceArray[4] = Math.Sqrt(xdisArray[4] + ydisArray[4] + zdisArray[4]);

                    //xdisArray[5] = Math.Pow(realCords[(row - 3) * depthWidth + (col - 3)].x * 1000 - ThereDCoordListDesc[i].x, 2);
                    //ydisArray[5] = Math.Pow(realCords[(row - 3) * depthWidth + (col - 3)].y * 1000 - ThereDCoordListDesc[i].y, 2);
                    //zdisArray[5] = Math.Pow(realCords[(row - 3) * depthWidth + (col - 3)].z * 1000 - ThereDCoordListDesc[i].z, 2);
                    //potDistanceArray[5] = Math.Sqrt(xdisArray[5] + ydisArray[5] + zdisArray[5]);

                    potDistanceArray[0] = Math.Abs(realCords[row * depthWidth + (col - 1)].z * 1000 - ThereDCoordListDesc[i].z);
                    potDistanceArray[1] = Math.Abs(realCords[row * depthWidth + (col - 2)].z * 1000 - ThereDCoordListDesc[i].z);
                    potDistanceArray[2] = Math.Abs(realCords[row * depthWidth + (col + 1)].z * 1000 - ThereDCoordListDesc[i].z);
                    potDistanceArray[3] = Math.Abs(realCords[row * depthWidth + (col + 2)].z * 1000 - ThereDCoordListDesc[i].z);
                    potDistanceArray[4] = Math.Abs(realCords[(row - 1) * depthWidth + col].z * 1000 - ThereDCoordListDesc[i].z);
                    potDistanceArray[5] = Math.Abs(realCords[(row - 1) * depthWidth + col].z * 1000 - ThereDCoordListDesc[i].z);

                    if (potDistanceArray[0] <= FilterDistanceConstant && potDistanceArray[1] <= FilterDistanceConstant && potDistanceArray[2] <= FilterDistanceConstant && potDistanceArray[3] <= FilterDistanceConstant && potDistanceArray[4] <= FilterDistanceConstant && potDistanceArray[5] <= FilterDistanceConstant)
                    {
                        yp0Index = Convert.ToInt32(row * depthWidth + col);
                        xp0 = ThereDCoordListDesc[i].x;
                        yp0 = ThereDCoordListDesc[i].y;
                        zp0 = ThereDCoordListDesc[i].z;
                        Console.WriteLine(xp0 + " " + yp0 + " " + zp0);
                        break;
                    }
                }
            }
        }
    }
}
