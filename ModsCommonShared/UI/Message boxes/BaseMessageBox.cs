﻿using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModsCommon.UI
{
    public abstract class MessageBoxBase : CustomUIPanel, IAutoLayoutPanel
    {
        public static float DefaultWidth => 573f;
        public static float DefaultHeight => 200f;
        public static float ButtonHeight => 47f;
        public static int Padding => 16;
        public static float MaxContentHeight => 500f;
        private static float ButtonsSpace => 25f;

        public static T ShowModal<T>()
        where T : MessageBoxBase
        {
            var uiObject = new GameObject();
            uiObject.transform.parent = UIView.GetAView().transform;
            var messageBox = uiObject.AddComponent<T>();

            UIView.PushModal(messageBox);
            messageBox.Show(true);
            messageBox.Focus();

            var view = UIView.GetAView();

            if (view.panelsLibraryModalEffect != null)
            {
                view.panelsLibraryModalEffect.FitTo(null);
                if (!view.panelsLibraryModalEffect.isVisible || view.panelsLibraryModalEffect.opacity != 1f)
                {
                    view.panelsLibraryModalEffect.Show(false);
                    ValueAnimator.Animate("ModalEffect67419", delegate (float val)
                    {
                        view.panelsLibraryModalEffect.opacity = val;
                    }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
                }
            }

            return messageBox;
        }
        public static void HideModal(MessageBoxBase messageBox)
        {
            UIView.PopModal();

            var view = UIView.GetAView();
            if (view.panelsLibraryModalEffect != null)
            {
                if (!UIView.HasModalInput())
                {
                    ValueAnimator.Animate("ModalEffect67419", delegate (float val)
                    {
                        view.panelsLibraryModalEffect.opacity = val;
                    }, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), delegate ()
                    {
                        view.panelsLibraryModalEffect.Hide();
                    });
                }
                else
                {
                    view.panelsLibraryModalEffect.zOrder = UIView.GetModalComponent().zOrder - 1;
                }
            }

            messageBox.Hide();
            Destroy(messageBox.gameObject);
        }

        private CustomUIDragHandle Header { get; set; }
        private CustomUILabel Caption { get; set; }
        protected AutoSizeAdvancedScrollablePanel Panel { get; set; }
        private CustomUIPanel ButtonPanel { get; set; }

        private List<uint> ButtonsRatio { get; } = new List<uint>();
        public string CaptionText { set => Caption.text = value; }
        public int DefaultButton { get; set; } = 1;

        #region CONSTRUCTOR

        public MessageBoxBase()
        {
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3((GetUIView().fixedWidth - width) / 2, (GetUIView().fixedHeight - height) / 2);
            size = new Vector2(DefaultWidth, DefaultHeight);
            color = new Color32(58, 88, 104, 255);
            backgroundSprite = "MenuPanel";

            AddHeader();
            AddContent();
            AddButtonPanel();

            SetSize();
        }
        private void AddHeader()
        {
            Header = AddUIComponent<CustomUIDragHandle>();
            Header.size = new Vector2(DefaultWidth, 42);
            Header.relativePosition = new Vector2(0, 0);
            Header.eventSizeChanged += (component, size) =>
            {
                Caption.size = size;
                Caption.CenterToParent();
            };

            Caption = Header.AddUIComponent<CustomUILabel>();
            Caption.textAlignment = UIHorizontalAlignment.Center;
            Caption.textScale = 1.3f;
            Caption.anchor = UIAnchorStyle.Top;

            Caption.eventTextChanged += (component, text) => Caption.CenterToParent();

            var cancel = Header.AddUIComponent<CustomUIButton>();
            cancel.normalBgSprite = "buttonclose";
            cancel.hoveredBgSprite = "buttonclosehover";
            cancel.pressedBgSprite = "buttonclosepressed";
            cancel.size = new Vector2(32, 32);
            cancel.relativePosition = new Vector2(527, 4);
            cancel.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Close();
        }
        private void AddContent()
        {
            Panel = AddUIComponent<AutoSizeAdvancedScrollablePanel>();
            Panel.MaxSize = new Vector2(DefaultWidth, MaxContentHeight);
            Panel.relativePosition = new Vector2(0, Header.height);
            Panel.Content.autoLayoutPadding = new RectOffset(Padding, Padding, 0, 0);
            Panel.eventSizeChanged += ContentSizeChanged;
        }
        private void AddButtonPanel()
        {
            ButtonPanel = AddUIComponent<CustomUIPanel>();
            ButtonPanel.size = new Vector2(DefaultWidth, ButtonHeight + 10);
        }

        #endregion

        #region EVENTS

        private Vector2 SizeBefore { get; set; } = new Vector2();
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            var view = GetUIView();
            var delta = (size - SizeBefore) / 2;
            SizeBefore = size;

            var x = Mathf.Clamp(relativePosition.x - delta.x, 0f, view.fixedWidth - size.x);
            var y = Mathf.Clamp(relativePosition.y - delta.y, 0f, view.fixedHeight - size.y);

            relativePosition = new Vector2(x, y);
        }

        private void ContentSizeChanged(UIComponent component, Vector2 value) => SetSize();
        private void SetSize()
        {
            height = Mathf.Floor(Header.height + Panel.height + ButtonPanel.height + Padding);
            ButtonPanel.relativePosition = new Vector2(0, Header.height + Panel.height + Padding);
        }

        #endregion

        #region BUTTONS

        protected CustomUIButton AddButton(Action action, uint ratio = 1)
        {
            var button = ButtonPanel.AddUIComponent<CustomUIButton>();
            button.normalBgSprite = "ButtonMenu";
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.horizontalAlignment = UIHorizontalAlignment.Center;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => action?.Invoke();

            ButtonsRatio.Add(Math.Max(ratio, 1));
            ChangeButtons();

            return button;
        }
        public void SetButtonsRatio(params uint[] ratio)
        {
            for (var i = 0; i < ButtonsRatio.Count; i += 1)
                ButtonsRatio[i] = i < ratio.Length ? Math.Max(ratio[i], 1) : 1;

            ChangeButtons();
        }
        public void ChangeButtons()
        {
            var sum = 0u;
            var before = ButtonsRatio.Select(i => (sum += i) - i).ToArray();

            var buttons = ButtonPanel.components.OfType<CustomUIButton>().ToArray();
            for (var i = 0; i < buttons.Length; i += 1)
                ChangeButton(buttons[i], i + 1, buttons.Length, (float)before[i] / sum, (float)ButtonsRatio[i] / sum);
        }
        private void ChangeButton(CustomUIButton button, int i, int from, float? positionRatio = null, float? widthRatio = null)
        {
            var width = this.width - (ButtonsSpace * 2 + ButtonsSpace / 2 * (from - 1));
            button.size = new Vector2(width * (widthRatio ?? 1f / from), ButtonHeight);
            button.relativePosition = new Vector2(ButtonsSpace * (0.5f + i / 2f) + width * (positionRatio ?? 1f / from * (i - 1)), 0);
        }

        #endregion

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (!p.used)
            {
                if (p.keycode == KeyCode.Escape)
                {
                    p.Use();
                    Close();
                }
                else if (p.keycode == KeyCode.Return)
                {
                    if (ButtonPanel.components.OfType<CustomUIButton>().Skip(DefaultButton - 1).FirstOrDefault() is CustomUIButton button)
                        button.SimulateClick();
                    p.Use();
                }
            }
        }

        protected virtual void Close() => HideModal(this);
        public void StopLayout() => Panel.StopLayout();
        public void StartLayout(bool layoutNow = true) => Panel.StartLayout(layoutNow);
    }
}
