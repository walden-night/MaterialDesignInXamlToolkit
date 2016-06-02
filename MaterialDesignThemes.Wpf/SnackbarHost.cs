﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MaterialDesignThemes.Wpf
{
    public class SnackbarHost : ContentControl
    {
        public const string SnackbarHostRootName = "SnackbarHostRoot";

        private static readonly HashSet<SnackbarHost> LoadedInstances = new HashSet<SnackbarHost>();

        public static readonly DependencyProperty IdentifierProperty = DependencyProperty.Register(nameof(Identifier), typeof(object), typeof(SnackbarHost), new PropertyMetadata(default(object)));

        /// <summary>
        /// Identifier which is used in conjunction with <see cref="Show(object)"/> to determine where a dialog should be shown.
        /// </summary>
        public object Identifier
        {
            get {
                return GetValue(IdentifierProperty);
            }

            set {
                SetValue(IdentifierProperty, value);
            }
        }

        private Grid _snackbarHostRoot;

        static SnackbarHost()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SnackbarHost), new FrameworkPropertyMetadata(typeof(SnackbarHost)));
        }

        public SnackbarHost()
        {
            Loaded += LoadedHandler;
            Unloaded += UnloadedHandler;
        }

        public override void OnApplyTemplate()
        {
            _snackbarHostRoot = (Grid)GetTemplateChild(SnackbarHostRootName);

            base.OnApplyTemplate();
        }

        private void LoadedHandler(object sender, RoutedEventArgs routedEventArgs)
        {
            LoadedInstances.Add(this);
        }

        private void UnloadedHandler(object sender, RoutedEventArgs routedEventArgs)
        {
            LoadedInstances.Remove(this);
        }

        public static async Task ShowAsync(string message)
        {
            await ShowAsync(null, message, null);
        }

        public static async Task ShowAsync(object hostIdentifier, string message)
        {
            await ShowAsync(hostIdentifier, message, null);
        }

        public static async Task ShowAsync(string message, SnackbarAction snackbarAction)
        {
            await ShowAsync(null, message, snackbarAction);
        }

        public static async Task ShowAsync(object hostIdentifier, string message, SnackbarAction snackbarAction)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Please provide a non empty value for " + nameof(message) + ".");
            }

            if (LoadedInstances.Count == 0)
            {
                throw new InvalidOperationException("No loaded SnackbarHost instances.");
            }

            LoadedInstances.First().Dispatcher.VerifyAccess();

            List<SnackbarHost> hosts = LoadedInstances.Where(host => Equals(host.Identifier, hostIdentifier)).ToList();

            if (hosts.Count == 0)
            {
                throw new InvalidOperationException("No loaded SnackbarHost have an Identifier property matching " + nameof(hostIdentifier) + " argument.");
            }

            if (hosts.Count > 1)
            {
                throw new InvalidOperationException("Multiple viable SnackbarHost. Specify a unique Identifier on each SnackbarHost, especially where multiple Windows are a concern.");
            }

            if (snackbarAction != null && string.IsNullOrWhiteSpace(snackbarAction.ActionLabel))
            {
                throw new ArgumentException("Please provide an actionLabel.");
            }

            await hosts[0].ShowInternalAsync(message, snackbarAction?.ActionLabel, snackbarAction?.ActionHandler);
        }

        private async Task ShowInternalAsync(string message, string actionLabel, SnackbarActionEventHandler actionHandler)
        {
            // find other visible Snackbars
            List<Snackbar> visibleSnackbars = _snackbarHostRoot.Children.OfType<Snackbar>().ToList();

            // if there is one, first hide it (should be at most one)
            visibleSnackbars.ForEach(async visibleSnackbar => await visibleSnackbar.Hide());

            // create a new Snackbar, place it inside the host and show it
            Snackbar snackbar = new Snackbar() { Message = message, ActionLabel = actionLabel, ActionHandler = actionHandler };
            _snackbarHostRoot.Children.Add(snackbar);

            // a very short delay, otherwise the visual states and the transitions will not work
            await Task.Delay(10);

            await snackbar.Show();
        }

        internal void RemoveSnackbar(Snackbar snackbar)
        {
            _snackbarHostRoot.Children.Remove(snackbar);
        }
    }
}
