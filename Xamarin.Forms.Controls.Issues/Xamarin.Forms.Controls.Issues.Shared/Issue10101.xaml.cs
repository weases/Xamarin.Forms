using Xamarin.Forms;
using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;
#if UITEST
using Xamarin.UITest;
using NUnit.Framework;
#endif

namespace Xamarin.Forms.Controls.Issues
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Github, 10101, "Changing text on a Label on macOS resets font color to Black", PlatformAffected.macOS)]
	public partial class Issue10101 : TestContentPage
	{
#if APP
		public Issue10101()
		{
			InitializeComponent();
		}

		protected override void Init()
		{
			
		}

		void button_Clicked(System.Object sender, System.EventArgs e)
		{
			label.Text = "This text should still be red";
		}
#endif

	}
}