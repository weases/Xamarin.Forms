
using System;
using Android.Content;
#if __ANDROID_29__
using Google.Android.Material.TextField;
#else
using Android.Support.Design.Widget;
#endif
using Xamarin.Forms.Platform.Android;

namespace Xamarin.Forms.Material.Android
{
	internal static class MaterialFormsEditTextManager
	{
		public static void Init(TextInputEditText textInputEditText)
		{
		}

		public static void Dispose(TextInputEditText textInputEditText)
		{
		}
	}
}