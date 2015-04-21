﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xbim.COBieLiteUK;
using Xbim.CobieLiteUK.Validation;
using Xbim.CobieLiteUK.Validation.Reporting;
using Xbim.WindowsUI.DPoWValidation.Extensions;
using Xbim.WindowsUI.DPoWValidation.Properties;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new ValidationViewModel();
            LoadSettings(vm);
            ValidationGrid.DataContext = vm;
        }

        private static void LoadSettings(ValidationViewModel vm)
        {
            if (File.Exists(Settings.Default.LastOpenedRequirement))
                vm.RequirementFileSource = Settings.Default.LastOpenedRequirement;
            if (File.Exists(Settings.Default.LastOpenedSubmission))
                vm.SubmissionFileSource = Settings.Default.LastOpenedSubmission;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (!(DataContext is ValidationViewModel)) 
                return;
            var vm = DataContext as ValidationViewModel;
            Settings.Default.LastOpenedRequirement = vm.RequirementFileSource;
            Settings.Default.LastOpenedSubmission = vm.SubmissionFileSource;
            Settings.Default.Save();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private Facility f;

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            f = new Facility();
            FileInfo flogger = new FileInfo("tmp");
            using (var logger = flogger.CreateText())
            {
                
                f.ValidateUK2012(logger, true);
            }            
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            string log;
            f.WriteCobie("fixed.xls", out log);
        }
    }
}