
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Material.Android;
using Xamarin.Forms.Platform.Android;
using AView = Android.Views.View;

namespace Xamarin.Forms.Material.Android
{
	public class MaterialEntryRenderer : EntryRendererBase<MaterialFormsTextInputLayout>, ITabStop
	{
		MaterialFormsEditText _textInputEditText;
		MaterialFormsTextInputLayout _textInputLayout;

		public MaterialEntryRenderer(Context context) :
			base(MaterialContextThemeWrapper.Create(context))
		{
		}

		public override SizeRequest GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			SizeRequest value;
			if (!string.IsNullOrWhiteSpace(_textInputEditText.Text))
			{
				// The material entry will measure to the size of the text it contains which causes 
				// some really weird unexpected layout behaviors.
				var text = _textInputEditText.Text;
				_textInputEditText.Text = string.Empty;
				value = base.GetDesiredSize(widthConstraint, heightConstraint);
				_textInputEditText.Text = text;
			}
			else
				value = base.GetDesiredSize(widthConstraint, heightConstraint);

			return value;
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}

		protected override AView ControlUsedForAutomation => EditText;

		protected override MaterialFormsTextInputLayout CreateNativeControl()
		{
			LayoutInflater inflater = LayoutInflater.FromContext(Context);
			var view = inflater.Inflate(Resource.Layout.TextInputLayoutFilledBox, null);
			_textInputLayout = (MaterialFormsTextInputLayout)view;
			_textInputEditText = _textInputLayout.FindViewById<MaterialFormsEditText>(Resource.Id.materialformsedittext);

			return _textInputLayout;
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);
			UpdateBackgroundColor();
		}

		protected override void UpdateColor() => ApplyTheme();

		protected override void UpdateBackgroundColor()
		{
			if (_textInputLayout == null)
				return;

			_textInputLayout.BoxBackgroundColor = MaterialColors.CreateEntryFilledInputBackgroundColor(Element.BackgroundColor, Element.TextColor);
		}

		protected override void UpdatePlaceHolderText() => _textInputLayout.SetHint(Element.Placeholder, Element);
		protected override EditText EditText => _textInputEditText;
		protected override void UpdatePlaceholderColor() => ApplyTheme();
		void ApplyTheme(Color textColor) => _textInputLayout?.ApplyTheme(textColor, Element.PlaceholderColor);
		void ApplyTheme() => ApplyTheme(Element.TextColor);

		protected override void UpdateTextColor(Color color)
		{
			ApplyTheme(color);
		}

		protected override void UpdateFont()
		{
			base.UpdateFont();
			_textInputLayout.Typeface = Element.ToTypeface();
			_textInputEditText.SetTextSize(ComplexUnitType.Sp, (float)Element.FontSize);
		}

		AView ITabStop.TabStop => EditText;
	}
}
