using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IntelPerceptualCameraDemo
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            PXCMSession session = null;
            pxcmStatus sts = PXCMSession.CreateInstance(out session);  //创建实例，建立会话 
            if (sts >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                Application.Run(new MainForm(session));
                session.Dispose();
            }
        }
    }
}
