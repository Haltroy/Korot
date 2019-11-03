﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Shell;
using System.Windows.Forms;

namespace Korot
{
    public class MyJumplist
    {
        private JumpList list;
        frmMain anaform;
        /// <summary>
        /// Creating a JumpList for the application
        /// </summary>
        /// <param name="windowHandle"></param>
        public MyJumplist(IntPtr windowHandle,frmMain MainForm)
        {
            anaform = MainForm;
            list = JumpList.CreateJumpListForIndividualWindow(TaskbarManager.Instance.ApplicationId, windowHandle);
            list.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent;
            BuildList();

        }

        /// <summary>
        /// Builds the Jumplist
        /// </summary>
        private void BuildList()
        {
            try
            {
                JumpListCustomCategory userActionsCategory = new JumpListCustomCategory("Actions");


                userActionsCategory.AddJumpListItems();
                list.AddCustomCategories(userActionsCategory);

                string incmodepath = Application.ExecutablePath + " -incognito";
                JumpListLink jlNotepad = new JumpListLink(incmodepath, anaform.newincwindow);
                jlNotepad.IconReference = new IconReference(Application.ExecutablePath, 0);

                string newwindow = Application.ExecutablePath;
                JumpListLink jlCalculator = new JumpListLink(newwindow, anaform.newwindow);
                jlCalculator.IconReference = new IconReference(Application.ExecutablePath, 0);

                list.AddUserTasks(jlNotepad);
                list.AddUserTasks(jlCalculator);
                list.Refresh();
            }catch { } //Ignore
        }
    }
}