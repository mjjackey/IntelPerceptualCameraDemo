using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntelPerceptualCameraDemo
{
    class Projection : IDisposable
    {
        private PXCMProjection projection=null;
        private float[] invalids = new float[2]; /* invalid depth values */
        public event EventHandler<EventArgs> ImageToRealWorldEvent;
        RenderStreams rs;

        public Projection(PXCMSession session, PXCMCapture.Device device,RenderStreams rs) {
            /* retrieve the invalid depth pixel values */
            device.QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SATURATION_VALUE, out invalids[0]);
            device.QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_LOW_CONFIDENCE_VALUE, out invalids[1]);

            int uid = 0; /* Create the projection instance */
            device.QueryPropertyAsUID(PXCMCapture.Device.Property.PROPERTY_PROJECTION_SERIALIZABLE, out uid); // Projection only
            session.DynamicCast<PXCMMetadata>(PXCMMetadata.CUID).CreateSerializable<PXCMProjection>(uid, PXCMProjection.CUID, out projection);

            this.rs = rs;

            ImageToRealWorldEvent += new EventHandler<EventArgs>(Projection_ImageToRealWorldEvent);
        }

        void Projection_ImageToRealWorldEvent(object sender, EventArgs e)
        {
            
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (projection==null) return;
            projection.Dispose();
            projection = null;
        }

        public PXCMPoint3DF32[] DepthToRealWord(PXCMImage depth)
        {
            /* Retrieve the depth pixels*/
            int dwidth = RenderStreams.ALIGN16(depth.info.width); /* aligned width */
            int dheight = (int)depth.info.height;
            PXCMImage.ImageData ddata;
            short[] dpixels;
            bool isdepth = (depth.info.format == PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH);  //** ???
            if (depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, out ddata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                dpixels = ddata.ToShortArray(0, isdepth ? dwidth * dheight : dwidth * dheight * 3);
                depth.ReleaseAccess(ref ddata);
            }
            else
            {
                dpixels = new short[isdepth ? dwidth * dheight : dwidth * dheight * 3];
            }

            /* Projection Calculation */
            PXCMPoint3DF32[] dcords = new PXCMPoint3DF32[dwidth * dheight];
            for (int y = 0, k = 0; y < dheight; y++)
            {
                for (int x = 0; x < dwidth; x++, k++)  //按行扫描
                {
                    dcords[k].x = x;
                    dcords[k].y = y;
                    dcords[k].z = isdepth ? dpixels[k] : dpixels[3 * k + 2];  //**Z的填充
                }
            }
            PXCMPoint3DF32[] realCords = new PXCMPoint3DF32[dwidth * dheight];
            pxcmStatus pImageToRealWordStatus = projection.ProjectImageToRealWorld(dcords, realCords);
            if (pImageToRealWordStatus >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {

            }

            //int i = 0;  //**MJ
            //for (i = 0; i < realCords.Length; i++)
            //{
            //    if (realCords[i].x == 0 && realCords[i].y == 0 && realCords[i].z == 0)
            //        continue;
            //    else
            //        break;
            //}
            //if (i < realCords.Length)  //数组内容不全为零  
            //    rs.depthToRealWordEvent.Set();
            return realCords;
        }
    }
}
