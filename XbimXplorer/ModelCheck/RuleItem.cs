using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace XbimXplorer.ModelCheck
{
    public class RuleItem: INotifyPropertyChanged
    {
        #region Data

        bool? _isChecked = false;
        RuleItem _parent;

        #endregion // Data

        #region CreateFoos

        public static List<RuleItem> CreateFoos()
        {
            RuleItem root = new RuleItem("Weapons")
            {
                IsInitiallySelected = true,
                Children =
                {
                    new RuleItem("Blades")
                    {
                        Children =
                        {
                            new RuleItem("Dagger"),
                            new RuleItem("Machete"),
                            new RuleItem("Sword"),
                        }
                    },
                    new RuleItem("Vehicles")
                    {
                        Children =
                        {
                            new RuleItem("Apache Helicopter"),
                            new RuleItem("Submarine"),
                            new RuleItem("Tank"),
                        }
                    },
                    new RuleItem("Guns")
                    {
                        Children =
                        {
                            new RuleItem("AK 47"),
                            new RuleItem("Beretta"),
                            new RuleItem("Uzi"),
                        }
                    },
                }
            };

            root.Initialize();
            return new List<RuleItem> { root };
        }

        RuleItem(string name)
        {
            this.Name = name;
            this.Children = new List<RuleItem>();
        }

        void Initialize()
        {
            foreach (RuleItem child in this.Children)
            {
                child._parent = this;
                child.Initialize();
            }
        }

        #endregion // CreateFoos

        #region Properties

        public List<RuleItem> Children { get; private set; }

        public bool IsInitiallySelected { get; private set; }

        public string Name { get; private set; }

        #region IsChecked

        /// <summary>
        /// Gets/sets the state of the associated UI toggle (ex. CheckBox).
        /// The return value is calculated based on the check state of all
        /// child RuleItems.  Setting this property to true or false
        /// will set all children to the same check state, and setting it 
        /// to any value will cause the parent to verify its check state.
        /// </summary>
        public bool? IsChecked
        {
            get { return _isChecked; }
            set { this.SetIsChecked(value, true, true); }
        }

        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _isChecked)
                return;

            _isChecked = value;

            if (updateChildren && _isChecked.HasValue)
                this.Children.ForEach(c => c.SetIsChecked(_isChecked, true, false));

            if (updateParent && _parent != null)
                _parent.VerifyCheckState();

            this.OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool? current = this.Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            this.SetIsChecked(state, false, true);
        }

        #endregion // IsChecked

        #endregion // Properties

        #region INotifyPropertyChanged Members

        void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
