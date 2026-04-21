using GVisionWpf.Api;
using GVisionWpf.UIs.DrawingObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// RoiDataListPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RoiDataListPanel : UserControl
    {
        #region Singleton

        private static RoiDataListPanel? _instance;
        public static RoiDataListPanel? Instance => _instance;

        #endregion

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<Roi>), typeof(RoiDataListPanel), new PropertyMetadata(null));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(RoiDataListPanel), new PropertyMetadata("ROI"));
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(EColor), typeof(RoiDataListPanel), new PropertyMetadata(EColor.Gold));

        private readonly List<DrawingObjectRoiWithText> drawingObjectTextRois = new List<DrawingObjectRoiWithText>(16);
        private int roiCount;

        #region Property
        public ObservableCollection<Roi> ItemsSource
        {
            get { return (ObservableCollection<Roi>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public EColor Color
        {
            get => (EColor)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        #endregion

        public RoiDataListPanel()
        {
            InitializeComponent();
            _instance = this;
        }

        ~RoiDataListPanel()
        {
            deleteAll();
        }

        #region 리플렉션 기반 메서드 실행

        /// <summary>
        /// 리플렉션을 사용하여 ROI 작업 실행
        /// </summary>
        public ApiResult ExecuteRoiOperation(string operationName, Dictionary<string, object>? parameters = null)
        {
            try
            {
                var method = this.GetType().GetMethod(operationName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (method == null)
                {
                    return new ApiResult
                    {
                        Success = false,
                        Error = $"작업 '{operationName}'을 찾을 수 없습니다."
                    };
                }

                var methodParams = method.GetParameters();
                object?[] args;

                // 파라미터가 없는 경우 빈 배열 전달
                if (methodParams.Length == 0)
                {
                    args = Array.Empty<object>();
                }
                else
                {
                    args = new object?[methodParams.Length];

                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        string paramName = methodParams[i].Name ?? "";

                        // 파라미터 딕셔너리에 값이 있으면 사용
                        if (parameters != null && parameters.ContainsKey(paramName))
                        {
                            args[i] = Convert.ChangeType(parameters[paramName],
                                Nullable.GetUnderlyingType(methodParams[i].ParameterType) ?? methodParams[i].ParameterType);
                        }
                        // 기본값이 있으면 기본값 사용
                        else if (methodParams[i].HasDefaultValue)
                        {
                            args[i] = methodParams[i].DefaultValue;
                        }
                        // nullable 타입이면 null 전달
                        else if (methodParams[i].ParameterType.IsClass ||
                                 Nullable.GetUnderlyingType(methodParams[i].ParameterType) != null)
                        {
                            args[i] = null;
                        }
                        else
                        {
                            return new ApiResult
                            {
                                Success = false,
                                Error = $"필수 파라미터 '{paramName}'이(가) 누락되었습니다."
                            };
                        }
                    }
                }

                var result = method.Invoke(this, args);

                if (result is ApiResult apiResult)
                {
                    return apiResult;
                }

                return new ApiResult
                {
                    Success = true,
                    Message = $"작업 '{operationName}' 실행 완료"
                };
            }
            catch (TargetInvocationException ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"작업 실행 중 오류: {ex.InnerException?.Message ?? ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"작업 실행 실패: {ex.Message}"
                };
            }
        }

        #endregion

        #region API 메서드

        /// <summary>
        /// ROI 추가 작업
        /// </summary>
        public ApiResult AddRoiOperation(string roiName, double row, double col, double height, double width)
        {
            try
            {
                Roi roi = new Roi(roiName, row, col, height, width);
                CreateRoi(roi);
                this.roiCount++;

                return new ApiResult
                {
                    Success = true,
                    Message = $"ROI 추가 완료: {roiName}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"ROI 추가 실패: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ROI 삭제 작업
        /// </summary>
        public ApiResult DeleteRoiOperation(int? index = null)
        {
            try
            {
                if (ItemsSource is not ObservableCollection<Roi> collection)
                {
                    return new ApiResult
                    {
                        Success = false,
                        Error = "ItemsSource가 올바르지 않습니다."
                    };
                }

                int targetIndex = index ?? this.xRoiDataGrid.SelectedIndex;

                if (targetIndex < 0 || targetIndex >= collection?.Count)
                {
                    return new ApiResult
                    {
                        Success = false,
                        Error = "선택된 ROI가 없거나 인덱스가 유효하지 않습니다."
                    };
                }

                this.drawingObjectTextRois[targetIndex].Delete();
                this.drawingObjectTextRois.RemoveAt(targetIndex);
                collection?.RemoveAt(targetIndex);

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    this.xRoiDataGrid.SelectedIndex = (targetIndex < this.drawingObjectTextRois.Count) ? targetIndex : this.drawingObjectTextRois.Count - 1;
                });

                return new ApiResult
                {
                    Success = true,
                    Message = $"ROI 삭제 완료 (인덱스: {targetIndex})"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"ROI 삭제 실패: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ROI 리셋 작업
        /// </summary>
        public ApiResult ResetRoisOperation()
        {
            try
            {
                ObservableCollection<Roi>? collection = ItemsSource as ObservableCollection<Roi>;

                deleteAll();
                collection?.Clear();
                this.drawingObjectTextRois.Clear();
                this.roiCount = 0;

                return new ApiResult
                {
                    Success = true,
                    Message = "모든 ROI 리셋 완료"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"ROI 리셋 실패: {ex.Message}"
                };
            }
        }
        public ApiResult UpdateRoiOperation(int index, double? row = null, double? col = null, double? height = null, double? width = null, string? roiName = null)
        {
            try
            {
                if (ItemsSource is not ObservableCollection<Roi> collection)
                {
                    return new ApiResult
                    {
                        Success = false,
                        Error = "ItemsSource가 올바르지 않습니다."
                    };
                }

                if (index < 0 || index >= collection.Count)
                {
                    return new ApiResult
                    {
                        Success = false,
                        Error = "인덱스가 유효하지 않습니다."
                    };
                }

                Roi roiToUpdate = collection[index];

                if (roiName != null)
                {
                    roiToUpdate.Name = roiName;
                }
                if (row.HasValue)
                {
                    roiToUpdate.Row1 = row.Value;
                }
                if (col.HasValue)
                {
                    roiToUpdate.Col1 = col.Value;
                }
                if (height.HasValue)
                {
                    roiToUpdate.Row2 = roiToUpdate.Row1 + height.Value;
                }
                if (width.HasValue)
                {
                    roiToUpdate.Col2 = roiToUpdate.Col1 + width.Value;
                }

                rebuildDrawingObject();
                this.xRoiDataGrid.SelectedIndex = index;


                return new ApiResult
                {
                    Success = true,
                    Message = $"ROI 업데이트 완료 (인덱스: {index})"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    Success = false,
                    Error = $"ROI 업데이트 실패: {ex.Message}"
                };
            }
        }
        #endregion

        #region 버튼 클릭 이벤트

        private void roiAddButton_Click(object sender, RoutedEventArgs e)
        {
            this.roiCount++;
            var result = AddRoiOperation("ROI " + this.roiCount, 500, 500, 1000, 1000);
            if (!result.Success)
            {
                MessageBox.Show(result.Error, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void roiDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = DeleteRoiOperation();
            if (!result.Success)
            {
                MessageBox.Show(result.Error, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void roiResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = ResetRoisOperation();
            if (!result.Success)
            {
                MessageBox.Show(result.Error, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void deleteAll()
        {
            foreach (DrawingObjectRoiWithText drawingObject in this.drawingObjectTextRois)
            {
                drawingObject.Detach();
                drawingObject.Delete();
            }
        }

        public void DetachAll()
        {
            foreach (DrawingObjectRoiWithText drawingObject in this.drawingObjectTextRois)
            {
                drawingObject.Detach();
            }
        }

        private void rebuildDrawingObject()
        {
            deleteAll();

            ObservableCollection<Roi>? collection = (ObservableCollection<Roi>?)ItemsSource;
            if (collection == null)
            {
                return;
            }

            List<Roi> itemSourceSnapShot = collection.ToList();
            collection.Clear();
            this.drawingObjectTextRois.Clear();

            foreach (Roi roi in itemSourceSnapShot)
            {
                CreateRoi(roi);
            }

            if (itemSourceSnapShot.Count == 0)
            {
                return;
            }

            string lastRoiName = itemSourceSnapShot[itemSourceSnapShot.Count - 1].Name;
            if (!int.TryParse(lastRoiName.Replace("ROI ", ""), out this.roiCount))
            {
                this.roiCount = itemSourceSnapShot.Count;
            }
        }

        public void AttachAll()
        {
            ObservableCollection<Roi> collection = (ObservableCollection<Roi>)ItemsSource;

            if (this.drawingObjectTextRois.Count != collection.Count)
            {
                rebuildDrawingObject();
            }

            foreach (DrawingObjectRoiWithText drawingObject in this.drawingObjectTextRois)
            {
                drawingObject.Attach();
            }
        }

        public void CreateRoi(Roi roi)
        {
            DrawingObjectRoiWithText drawingObject = new DrawingObjectRoiWithText(roi, Color);
            drawingObject.Create();
            drawingObject.Attach();
            this.drawingObjectTextRois.Add(drawingObject);

            var collection = ItemsSource as ObservableCollection<Roi>;
            collection?.Add(roi);

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.xRoiDataGrid.SelectedIndex = (this.xRoiDataGrid.SelectedIndex < 0) ? this.drawingObjectTextRois.Count - 1 : this.xRoiDataGrid.SelectedIndex;
            });
        }

        // FYI: SelectedIndex를 변경하면, CurrentCellChanged 이벤트가 호출됩니다.
        // DataGrid의 Cell의 Value가 변경되었을 때의 이벤트리스너를 사용하고 싶다면,
        // CellEditEnding을 사용하거나 값이 변화되었는지 검사하세요.
        private void onCellEditEnding(object? sender, EventArgs e)
        {
            int currentIndex = this.xRoiDataGrid.SelectedIndex;
            rebuildDrawingObject();
            this.xRoiDataGrid.SelectedIndex = currentIndex;
        }

        public void RebuildRoiList()
        {
            int currentIndex = this.xRoiDataGrid.SelectedIndex;
            rebuildDrawingObject();
            this.xRoiDataGrid.SelectedIndex = currentIndex < 0 ? 0 : currentIndex;
        }

        // aml
        public void ExecuteAction(string action)
        {
            Debug.WriteLine("ExecuteAction 들어옴");
            switch (action)
            {
                case "ADD":
                    roiAddButton_Click(this, new RoutedEventArgs());
                    break;

                case "DELETE":
                    roiDeleteButton_Click(this, new RoutedEventArgs());
                    break;

                case "RESET":
                    roiResetButton_Click(this, new RoutedEventArgs());
                    break;

                default:
                    Debug.WriteLine($"[RoiDataListPanel] Unknown action: {action}");
                    break;
            }
        }

    }
}