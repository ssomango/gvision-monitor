namespace GVisionWpf.UIs.ViewModels
{
    public class CurrentSettingViewmodel : ViewModelBase
    {
        private static readonly Lazy<CurrentSettingViewmodel> lazy = new Lazy<CurrentSettingViewmodel>(() => new CurrentSettingViewmodel());
        public static CurrentSettingViewmodel Instance => lazy.Value;

        private ESaveOption saveOption;
        private EColor saveOptionColor;

        private EInspectionMode inspectionMode;
        private EColor inspectionModeColor;

        private string recipeName;
        private EColor recipeNameColor;

        private string lotNumber;
        private EColor lotNumberColor;

        private CurrentSettingViewmodel()
        {
            this.saveOptionColor = EColor.Black;

            this.inspectionModeColor = EColor.Blue;

            this.recipeName = "NOT LOADING";
            this.recipeNameColor = EColor.Black;

            this.lotNumber = "NOT LOADING";
            this.lotNumberColor = EColor.Black;
        }

        #region Property

        public ESaveOption SaveOption
        {
            get => this.saveOption;
            set
            {
                SetField(ref this.saveOption, value);
            }
        }

        public EColor SaveOptionColor
        {
            get => this.saveOptionColor;
            set
            {
                SetField(ref this.saveOptionColor, value);
            }
        }

        public EInspectionMode InspectionMode
        {
            get => this.inspectionMode;
            set
            {
                SetField(ref this.inspectionMode, value);
            }
        }

        public EColor InspectionModeColor
        {
            get => this.inspectionModeColor;
            set => SetField(ref this.inspectionModeColor, value);
        }

        public string RecipeName
        {
            get => this.recipeName;
            set
            {
                SetField(ref this.recipeName, value);
            }
        }

        public EColor RecipeNameColor
        {
            get => this.recipeNameColor;
            set => SetField(ref this.recipeNameColor, value);
        }

        public string LotNumber
        {
            get => this.lotNumber;
            set
            {
                SetField(ref this.lotNumber, value);
            }
        }

        public EColor LotNumberColor
        {
            get => this.lotNumberColor;
            set => SetField(ref this.lotNumberColor, value);
        }

        #endregion
    }
}
