using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Symbol.XamarinEMDK;
using Android.Widget;
using Android.Util;
using System.Linq;

namespace DeviceIdentifiers
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, EMDKManager.IEMDKListener
    {
        private string SERIAL_URI = "content://oem_info/oem.zebra.secure/build_serial";
        private string IMEI_URI = "content://oem_info/wan/imei";
        private string BT_MAC_URI = "content://oem_info/oem.zebra.secure/bt_mac";

        private EMDKManager mEmdkManager = null;
        private ProfileManager mProfileManager = null;

        public string LOG_TAG { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            Button btnRequestPermissions = FindViewById<Button>(Resource.Id.btnRequestPermission);
            btnRequestPermissions.Click += btnRequestPermissionClick;
            Button btnGetDeviceIdentifiers = FindViewById<Button>(Resource.Id.btnGetDeviceIdentifiers);
            btnGetDeviceIdentifiers.Click += btnGetDeviceIdentifiersClick;


        }


        private void btnRequestPermissionClick(object sender, EventArgs e)
        {
            EMDKResults emdkManagerResults = EMDKManager.GetEMDKManager(Application.Context, this);
            if (emdkManagerResults.StatusCode != EMDKResults.STATUS_CODE.Success)
            {
                Log.Error(LOG_TAG, "Unable to create EMDK Manager");
                Toast.MakeText(this, "Unable to create EMDK Manager", ToastLength.Short).Show();
            }
        }

        private void btnGetDeviceIdentifiersClick(object sender, EventArgs e)
        {
            TextView txtSerial = FindViewById<TextView>(Resource.Id.txtSerial);
            TextView txtIMEI = FindViewById<TextView>(Resource.Id.txtIMEI);
            TextView txtBtMac = FindViewById<TextView>(Resource.Id.txtBtMac);

            RunOnUiThread(() =>
            {
                string serial = RetrieveDeviceIdentifier(SERIAL_URI);
                txtSerial.Text = "Serial: " + serial;
                string imei = RetrieveDeviceIdentifier(IMEI_URI);
                txtIMEI.Text = "IMEI: " + imei;
                string bt_mac = RetrieveDeviceIdentifier(BT_MAC_URI);
                txtBtMac.Text = "BT MAC: " + bt_mac;
            });
        }


        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private string RetrieveDeviceIdentifier(string identifierUri)
        {
            Android.Net.Uri uri = Android.Net.Uri.Parse(identifierUri);
            string data = "Error";
            var cursor = ApplicationContext.ContentResolver.Query(uri, null, null, null, null);
            if (cursor == null || cursor.Count < 1)
            {
                String errorMsg = "Could not read identifier.  Have you granted access?  Does this device support retrieval of this identifier?";
                Log.Error(LOG_TAG, errorMsg);
                return errorMsg;
            }
            else
            {
                while (cursor.MoveToNext())
                {
                    if (cursor.ColumnCount == 0)
                    {
                        //  No data in the cursor.  
                        String errorMsg = "Error: " + uri + " does not exist on this device";
                        return errorMsg;
                    }
                    else
                    {
                        for (int i = 0; i < cursor.ColumnCount; i++)
                        {
                            try
                            {
                                data = cursor.GetString(cursor.GetColumnIndex(cursor.GetColumnName(i)));
                            }
                            catch (Exception e)
                            {
                                return e.Message;
                            }
                        }
                    }
                }
                cursor.Close();
                return data;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        void EMDKManager.IEMDKListener.OnClosed()
        {
            mProfileManager = null;
            if (mEmdkManager != null)
            {
                mEmdkManager.Release();
                mEmdkManager = null;
            }
        }

        void EMDKManager.IEMDKListener.OnOpened(EMDKManager emdkManager)
        {
            mEmdkManager = emdkManager;
            mProfileManager = (ProfileManager)mEmdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Profile);
            if (mProfileManager != null)
            {
                EMDKResults result = mProfileManager.ProcessProfile("GrantSerialId", ProfileManager.PROFILE_FLAG.Set, 
                    generateProfileXml("GrantSerialId", SERIAL_URI));

                EMDKResults result2 = mProfileManager.ProcessProfile("GrantIMEIId", ProfileManager.PROFILE_FLAG.Set,
                    generateProfileXml("GrantIMEIId", IMEI_URI));

                EMDKResults result3 = mProfileManager.ProcessProfile("GrantBTMac", ProfileManager.PROFILE_FLAG.Set,
                    generateProfileXml("GrantBTMac", BT_MAC_URI));

                if ((result.StatusCode.Equals(EMDKResults.STATUS_CODE.CheckXml) || result.StatusCode.Equals(EMDKResults.STATUS_CODE.Success))
                    && (result2.StatusCode.Equals(EMDKResults.STATUS_CODE.CheckXml) || result2.StatusCode.Equals(EMDKResults.STATUS_CODE.Success))
                    && (result3.StatusCode.Equals(EMDKResults.STATUS_CODE.CheckXml) || result3.StatusCode.Equals(EMDKResults.STATUS_CODE.Success)))
                {
                    Log.Info(LOG_TAG, "EMDK Profiles successfully applied");
                    Toast.MakeText(this, "EMDK Profiles successfully applied", ToastLength.Long).Show();
                }
                else
                {
                    Log.Error(LOG_TAG, "EMDK Profiles could not be applied");
                    Toast.MakeText(this, "EMDK Profiles could not be applied", ToastLength.Long).Show();
                }

                mEmdkManager.Release();
                mEmdkManager = null;
            }
            else
            {
                Log.Error(LOG_TAG, "Unable to create Profile Manager");
                Toast.MakeText(this, "Unable to create Profile Manager", ToastLength.Short).Show();
            }
        }

        string[] generateProfileXml(string profileName, string serviceIdentifier)
        {
            string packageName = ApplicationContext.PackageName;
            string packageSignature = getPackageSignature(ApplicationContext);
            string xml = "<wap-provisioningdoc>" +
                                        "  <characteristic type=\"Profile\">" +
                                        "    <parm name=\"ProfileName\" value=\"" + profileName +"\"/>" +
                                        "    <characteristic version=\"8.3\" type=\"AccessMgr\">" +
                                        "      <parm name=\"OperationMode\" value=\"1\" />" +
                                        "      <parm name=\"ServiceAccessAction\" value=\"4\" />" +
                                        "      <parm name=\"ServiceIdentifier\" value=\"" + serviceIdentifier + "\" />" +
                                        "      <parm name=\"CallerPackageName\" value=" + '"' + packageName + '"' + " />" +
                                        "      <parm name=\"CallerSignature\" value=" + '"' + packageSignature + '"' + "  />" +
                                        "    </characteristic>" +
                                        "  </characteristic>" +
                                        "</wap-provisioningdoc>";
            string[] returnVal = new string[1];
            returnVal[0] = xml;
            return returnVal;
        }

        string getPackageSignature(Android.Content.Context context)
        {
            Android.Content.PM.Signature[] sigs;
            Android.Content.PM.SigningInfo signingInfo;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                signingInfo = context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.SigningCertificates).SigningInfo;

                sigs = signingInfo.GetApkContentsSigners();
            }
            else
            {
                sigs = (Android.Content.PM.Signature[])context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Signatures).Signatures;
            }
            char[] signatureAsChars = sigs[0].ToChars();
            byte[] decodeHex = StringToByteArray(new string(signatureAsChars));
            return Base64.EncodeToString(decodeHex, Base64Flags.NoWrap);
        }

        public static byte[] StringToByteArray(string hex)
        {
            //  https://stackoverflow.com/questions/24108005/convert-hex-string-to-byte-array
            if ((hex.Length % 2) != 0)
                hex = "0" + hex;

            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
