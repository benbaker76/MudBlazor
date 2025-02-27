// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions;
using MudBlazor.State;
using MudBlazor.Utilities;
using MudBlazor.Utilities.Throttle;

namespace MudBlazor
{
    /// <summary>
    /// Represents a sophisticated and customizable pop-up for choosing a color.
    /// </summary>
    public partial class MudGradientPicker : MudPicker<Gradient>
    {
        //private readonly ParameterState<int> _throttleIntervalState;

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private IJsApiService JsApiService { get; set; }

        public MudGradientPicker() : base(new DefaultConverter<Gradient>())
        {
            Adornment = Adornment.None;
            //AdornmentIcon = Icons.Material.Outlined.Gradient;
            ShowToolbar = false;
            //Value = GradientCollection.Gradients[0];
            //Text = Value.Name;
            AdornmentAriaLabel = "Open Color Picker";
            //if (_activeColorPickerView is not ColorPickerView.)
            //if (PickerVariant != PickerVariant.Static)
            //    Style = $"background: {Value.ToString()}";
            /* using var registerScope = CreateRegisterScope();
            _throttleIntervalState = registerScope.RegisterParameter<int>(nameof(ThrottleInterval))
                .WithParameter(() => ThrottleInterval)
                .WithChangeHandler(OnThrottleIntervalParameterChanged); */

            UserAttributes["id"] = _baseId;
        }

        #region Fields

        private const double _maxY = 250;
        private const double _maxX = 312;
        private const double _selectorSize = 26.0;

        private double _selectorX;
        private double _selectorY;

        private bool _collectionOpen;

        private readonly string _id = Identifier.Create();
        private string _baseId = Identifier.Create("mudinput");

        private ThrottleDispatcher _throttleDispatcher;

        #endregion

        #region Parameters

        /// <summary>
        /// The currently selected gradient as a <see cref="Gradient"/>.
        /// </summary>
        /// <remarks>
        /// When this value changes, the <see cref="ValueChanged"/> event occurs.
        /// </remarks>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Data)]
        public Gradient Value
        {
            get => _value;
            set => SetGradientAsync(value).CatchAndLog();
        }

        /// <summary>
        /// Occurs when the <see cref="Value"/> property has changed.
        /// </summary>
        [Parameter]
        public EventCallback<Gradient> ValueChanged { get; set; }

        /// <summary>
        /// Continues to update the selected color while the mouse button is down.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>true</c>.  When <c>false</c>, conditions like long latency are better supported and can be adjusted via the <see cref="ThrottleInterval"/> property.
        /// </remarks>
        [Parameter]
        [Category(CategoryTypes.FormComponent.PickerBehavior)]
        public bool DragEffect { get; set; } = true;

        /// <summary>
        /// The delay, in milliseconds, between updates to the selected color when <see cref="DragEffect"/> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>300</c> milliseconds between updates.
        /// </remarks>
        [Parameter]
        [Category(CategoryTypes.FormComponent.PickerBehavior)]
        public int ThrottleInterval { get; set; } = 300;

        #endregion

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
            //SetThrottle(_throttleIntervalState.Value);

            SetCurrentGradient(GradientCollection.Gradients[0]);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                UserAttributes.Remove("id");
                UserAttributes["class"] = "gradient-background";

                await UpdateGradient();

                await JSRuntime.InvokeVoidAsync("import", "./_content/MudBlazor/MudGradientPicker.js");
            }
        }

        private void OnThrottleIntervalParameterChanged(ParameterChangedEventArgs<int> args)
        {
            SetThrottle(args.Value);
        }

        private void SetThrottle(int interval)
        {
            _throttleDispatcher = interval > 0
                ? new ThrottleDispatcher(interval)
                : null;
        }

        private void ToggleCollection()
        {
            _collectionOpen = !_collectionOpen;
        }

        private async Task SelectGradientAsync(Gradient value)
        {
            Value = value;
            _collectionOpen = false;

            await CloseAsync();
        }

        private async Task SetGradientAsync(Gradient value)
        {
            if (value == null)
            {
                return;
            }

            _value = value;

            await UpdateGradient();
        }

        private async Task UpdateGradient()
        {
            if (!IsJSRuntimeAvailable)
                return;

            await JsApiService.UpdateStyleProperty(_baseId, "background", Value.ToString());
        }

        #region mouse interactions

        private async Task HandleColorOverlayClickedAsync()
        {
            //UpdateColorBaseOnSelection();

            await CloseAsync();
        }

        private Task OnColorOverlayClick(PointerEventArgs e)
        {
            SetSelectorBasedOnPointerEvents(e, true);

            return HandleColorOverlayClickedAsync();
        }

        //private async Task OnPointerMoveAsync(PointerEventArgs e)
        private void OnPointerMoveAsync(PointerEventArgs e)
        {
            if (e.Buttons == 1 && DragEffect)
            {
                SetSelectorBasedOnPointerEvents(e, true);

                if (_throttleDispatcher is null)
                {
                    // Update instantly because debounce is not enabled.
                    //UpdateColorBaseOnSelection();
                }
                else
                {
                    //await _throttleDispatcher.ThrottleAsync(() => InvokeAsync(UpdateColorBaseOnSelection));
                }
            }
        }

        private void SetSelectorBasedOnPointerEvents(PointerEventArgs e, bool offsetIsAbsolute)
        {
            _selectorX = (offsetIsAbsolute ? e.OffsetX : e.OffsetX - (_selectorSize / 2.0) + _selectorX).EnsureRange(_maxX);
            _selectorY = (offsetIsAbsolute ? e.OffsetY : e.OffsetY - (_selectorSize / 2.0) + _selectorY).EnsureRange(_maxY);
        }

        #endregion

        #region updating inputs

        /// <summary>
        /// Sets the selected color to the specified value.
        /// </summary>
        /// <param name="input">
        /// A string value formatted as hexadecimal (<c>#FF0000</c>), RGB (<c>rgb(255,0,0)</c>), or RGBA (<c>rgba(255,0,0,255)</c>.
        /// </param>
        public void SetInputString(string input)
        {
            Gradient gradient;
            try
            {
                gradient = GradientCollection.Gradients[0];
            }
            catch (Exception)
            {
                return;
            }

            Value = gradient;
        }

        protected override Task StringValueChangedAsync(string value)
        {
            SetInputString(value);
            return Task.CompletedTask;
        }

        #endregion

        #region helper

        //private string GetSelectorLocation() => $"translate({Math.Round(_selectorX, 2).ToString(CultureInfo.InvariantCulture)}px, {Math.Round(_selectorY, 2).ToString(CultureInfo.InvariantCulture)}px);";
        //private string GetColorTextValue() => (!ShowAlpha || _activeColorPickerView is ColorPickerView.Palette or ColorPickerView.GridCompact) ? _value.ToString(MudColorOutputFormats.Hex) : _value.ToString(MudColorOutputFormats.HexA);
        //private int GetHexColorInputMaxLength() => !ShowAlpha ? 7 : 9;

        private EventCallback<MouseEventArgs> GetEventCallback() => EventCallback.Factory.Create<MouseEventArgs>(this, () => CloseAsync());
        private EventCallback<MouseEventArgs> GetSelectGradientCallback(Gradient gradient) => new EventCallbackFactory().Create(this, (MouseEventArgs _) => SelectGradientAsync(gradient));

        //private Color GetButtonColor(ColorPickerView view) => _activeColorPickerView == view ? Color.Primary : Color.Inherit;
        //private string GetColorDotClass(Gradient gradient) => new CssBuilder("mud-picker-color-dot").AddClass("selected", gradient == Value).ToString();
        //private string AlphaSliderStyle => new StyleBuilder().AddStyle($"background-image: linear-gradient(to {(RightToLeft ? "left" : "right")}, transparent, {_value.ToString(MudColorOutputFormats.RGB)})").Build();

        #endregion
    }
}
