using System;
using System.Collections.Generic;
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

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class CharacterListItem: IComparable
    {
        private CombatEvent _Parent = null;
        private CombatEventList _Parents = new CombatEventList();
        private CharacterList _Children = new CharacterList();

        // I'm making these lists and persistant for troubleshooting purposes.  This way I can output the results of VFR into the application, rather than depending on debug statements.

        private CombatEventList _VFR_Friendly_CombatEvents = new CombatEventList();
        private CombatEventList _VFR_Hostile_CombatEvents = new CombatEventList();
        private CombatEventList _VFR_Other_CombatEvents = new CombatEventList();

        private CombatEventList _VFR_Damage_Friendly = new CombatEventList();
        private CombatEventList _VFR_Damage_Hostile = new CombatEventList();

        /// <summary>Fires when certain key fields (Character_Name, Friendly_Name, Character_Type, Target_Character_Name, Target_Friendly_Name, and Target_Character_Type) are updated for direct children</summary>
        public event CombatEventChanged OnCombatEventChanged;

        // These properties are overriden in TargetedCharacterListItem, thus the "virtual" tag.
        /// <summary>The Source_Character_Name that serves as the base for this CharacterListItem</summary>
        public virtual string Source_Character_Name { get => _Parent.Source_Character_Name; }
        /// <summary>The Character_Name (editable) that serves as the base for this CharacterListItem</summary>
        public virtual string Character_Name { get => _Parent.Character_Name; set => _Parent.Character_Name = value; }
        /// <summary>The Friendly_Name (editable) that serves as the base for this CharacterListItem</summary>
        public virtual string Friendly_Name { get => _Parent.Friendly_Name; set => _Parent.Friendly_Name = value; }
        /// <summary>The Character_List_item (editable) of this CharacterListItem.  Note that changing this value, while possible, is much more expensive than you might expect -- calling Set_Character_Type(CombatEvent.Char_Enum) is strongly recommended.</summary>
        public virtual CombatEvent.Char_Enum Character_Type
        {
            get => _Parent.Character_Type;
            set => Set_Character_Type(value);
        }

        /// <summary>A CombatEventList that contains all of the CombatEvents that *directly* support the existance of this CharacterListItem.</summary>
        public CombatEventList Parents { get => _Parents; }
        /// <summary>The CombatEvent with the lowest ID (== oldest) in the Parents collection.</summary>
        public CombatEvent Parent { get => _Parent; set => _Parent = value; }
        /// <summary>The CharacterList that contains all the Characters which are specific instances of this CharacterListItem (distinguished only by GUID)</summary>
        public CharacterList Children { get => _Children; }

        /// <summary>
        /// Generates a combined CombatEventList of both direct and indirect children, sorts the list by CombatEvent.ID (= chronological order, as determined by sequence within the original
        /// combatLog.txt file), then concatenates the CombatEvent.Source_With_ID values toether.
        /// 
        /// This results in a prettytyped log extract suitable for display in a WebBrowser containing all the data that is associated with a CharacterListItem.
        /// 
        /// Note:  This can result in a *very large* string, and considerable processing, if the number of CombatEvents attached to a character is lost.  ~2k doesn't cause any performance issues
        /// on my computer, but 10k likely would lead to issues.
        /// </summary>
        public string Source_With_ID
        {
            get
            {
                StringBuilder tmp_sb = new StringBuilder();
                CombatEventList tmp_lst = Get_All_CombatEvents();
                int curr_combat_ID = -1;
                tmp_lst.Sort((first, second) => first.ID.CompareTo(second.ID));
                foreach (CombatEvent curr_evnt in tmp_lst)
                {
                    if (curr_combat_ID == -1) { curr_combat_ID = curr_evnt.Combat_ID; }
                    if (curr_combat_ID != curr_evnt.Combat_ID) { curr_combat_ID = curr_evnt.Combat_ID; tmp_sb.Append("<hr>\n"); }
                    tmp_sb.Append(curr_evnt.Source_With_ID + "\n");
                }
                return tmp_sb.ToString();
            }
        }

        /// <summary>
        /// Adjust the Character_Type associated with all events associated with this CharacterListItem (both directly, via _Parents and indirectly, via _Children) to a specified value.
        /// 
        /// This knows that some of the attached events may be "CombatEventTargeted" events, where the target is the only reason this CombatEvent is included in the list.  In such cases,
        /// the "Target_Character_Type" is [also] updated, if required.
        /// </summary>
        /// <param name="in_Char_Type">The new CombatEvent.Char_Enum to be set</param>
        /// <returns>The number of CombatEvents that were changed</returns>
        public int Set_Character_Type(CombatEvent.Char_Enum in_Char_Type)
        {
            int changed_cnt = 0;

            foreach (CombatEvent curr_event in Parents)
            {
                if (curr_event.Source_Character_Name == Source_Character_Name)
                {
                    if (curr_event.Character_Type != in_Char_Type) { changed_cnt++; curr_event.Character_Type = in_Char_Type; curr_event.Guess_Character_Type = in_Char_Type; }
                }

                if (curr_event is CombatEventTargeted)
                {
                    CombatEventTargeted curr_tgt_event = (CombatEventTargeted)curr_event;
                    if (curr_tgt_event.Source_Target_Character_Name == Source_Character_Name)
                    {
                        if (curr_tgt_event.Target_Character_Type != in_Char_Type) { changed_cnt++; curr_tgt_event.Target_Character_Type = in_Char_Type; curr_tgt_event.Guess_Target_Character_Type = in_Char_Type;  }
                    }
                }
            }

            foreach (CharacterListItem curr_child in Children) { changed_cnt += curr_child.Set_Character_Type(in_Char_Type); }

            return changed_cnt;
        }

        /// <summary>
        /// Something of a leftover from previous iterations, this function combines the CombatEvents that are included in this CharacterList because they *target* the Character
        /// the list is describing and those that are included becuase the Character is the *actor* in an event.
        /// 
        /// For example, "Alice attacks Bob" belongs in Alice's CharacterListItem, because Alice was the actor, but it also belongs in Bob's, since he was the target.
        /// 
        /// Originally, these two lists were going to be stored seperately, and this method would allow them to be recombined.  Now, however, only a single consolidated list (in
        /// _Parents) is stored, and this method is superfluous and simply returns _Parents).  Its retained (rather than being deleted altogether) because it provides a place to 
        /// perform / output information on CombatEvents attached to a CharacterListItem if needed for troubleshooting.
        /// </summary>
        /// <returns>_Parents</returns>
        public CombatEventList Get_Combined_Parents()
        {/*  Useful for troubleshooting, but not necessary otherwise.
            CombatEventList rtn = new CombatEventList();

            int no_target_events_cnt = 0;
            int targeted_events_where_this_char_is_source = 0;
            int targeted_events_where_this_char_is_target = 0;

            foreach (CombatEvent curr_itm in _Parents)
            {
                rtn.Add(curr_itm);
                if (curr_itm is CombatEventTargeted)
                {
                    if (((CombatEventTargeted)curr_itm).Source_Target_Character_Name == Source_Character_Name) { targeted_events_where_this_char_is_target++; }
                    else { targeted_events_where_this_char_is_source++; }
                }
                else { no_target_events_cnt++; }
            }*/

            return _Parents;
        }

        /// <summary>
        /// This returns a list of *ALL* the CombatEvents associated with this Character, including the ones attached to other instances of this Character.
        /// 
        /// Note that this is a CPU extensive operation.
        /// </summary>
        /// <returns>A CombatEventList containing all the events attached to a character</returns>
        private CombatEventList Get_All_CombatEvents()
        {
            CombatEventList all_events = new CombatEventList();
            all_events.AddRange(Parents);
            foreach (CharacterListItem curr_child_char in Children) { all_events.AddRange(curr_child_char.Parents); }

            return all_events;
        }

        /// <summary>
        /// Interates over all CombatEventTargeted CombatEvents that are attached to this CharacterListItem and determines to which faction this character appears to belong.
        /// Once this scan is complete, the "votes" are tallied, and whichever faction recieved more votes is set at the faction of this character.
        /// 
        /// Note that this makes a determination as to which faction a character belongs all CombatEvents (not just CombatEventTargeted) are updated to reflect this new designation.
        /// This, by running it more than once on a group of characters can result in characters that were either indeterminate or misclassified on the first past being properly 
        /// classified on later passes.  Thus, it returns the number of CombatEvents that were reclassified
        /// </summary>
        /// <returns>An integer containing the number of CombatEvents whose faction was adjusted.  If this is 0, then no changes were made.</returns>
        public int Vote_For_Role()
        {
            int changed_cnt = 0;

            _VFR_Friendly_CombatEvents.Clear();
            _VFR_Hostile_CombatEvents.Clear();
            _VFR_Other_CombatEvents.Clear();

            _VFR_Damage_Friendly.Clear();
            _VFR_Damage_Hostile.Clear();

            changed_cnt=0;

            CombatEventList all_events = Get_All_CombatEvents();

            foreach (CombatEvent curr_evnt_tmp in all_events)
            {
                if (!(curr_evnt_tmp is CombatEventTargeted)) { continue; }

                CombatEventTargeted curr_evnt = (CombatEventTargeted)curr_evnt_tmp;
                if (curr_evnt.Friendly_Name == Friendly_Name)
                {
                    // If X = the character we are interested in, these are events of the type "X <verb> Y".  X is the source of the event.

                    CombatEvent.Char_Enum proposed_char_type = curr_evnt.Character_Type_From_Target();

                    if (proposed_char_type == CombatEvent.Char_Enum.Friendly)
                    {
                        if (curr_evnt is AttackEvent) { _VFR_Friendly_CombatEvents.Add(curr_evnt); }
                        else if (curr_evnt is HealingEvent) { _VFR_Friendly_CombatEvents.Add(curr_evnt); }
                        else if (curr_evnt is DamageEvent) { _VFR_Damage_Friendly.Add(curr_evnt); }
                        else { _VFR_Other_CombatEvents.Add(curr_evnt); }

                    }
                    else if (proposed_char_type == CombatEvent.Char_Enum.Hostile)
                    {
                        if (curr_evnt is AttackEvent) { _VFR_Hostile_CombatEvents.Add(curr_evnt); }
                        else if (curr_evnt is HealingEvent) { _VFR_Hostile_CombatEvents.Add(curr_evnt); }
                        else if (curr_evnt is DamageEvent) { _VFR_Damage_Hostile.Add(curr_evnt); }
                        else { _VFR_Other_CombatEvents.Add(curr_evnt); }
                    }
                    else { _VFR_Other_CombatEvents.Add(curr_evnt); }
                }
                else
                {
                    // These are "Y <verb> X", where again, X is the character we are interested in.

                    CombatEvent.Char_Enum proposed_target_char_type = curr_evnt.Character_Type_From_Target();

                    // Some processing is required here -- what we have is the *source* type, but what we want is the *target* type.

                    // Attacks are aimed at people of the opposing faction, generally.
                    // Heals are aimed at people of the same faction, generally.
                    // Damage is aimed at people of the opposing factiong, **very** generally.

                    if (proposed_target_char_type == CombatEvent.Char_Enum.Friendly)
                    {
                        if (curr_evnt is AttackEvent) { _VFR_Hostile_CombatEvents.Add(curr_evnt); }
                        else if (curr_evnt is HealingEvent) { _VFR_Friendly_CombatEvents.Add(curr_evnt); }
                        else if (curr_evnt is DamageEvent) { _VFR_Damage_Hostile.Add(curr_evnt); }
                        else { _VFR_Other_CombatEvents.Add(curr_evnt); }
                    }
                    else if (proposed_target_char_type == CombatEvent.Char_Enum.Hostile)
                    {
                        if (curr_evnt is AttackEvent) { _VFR_Friendly_CombatEvents.Add(curr_evnt); }
                        else if (curr_evnt is HealingEvent) { _VFR_Hostile_CombatEvents.Add(curr_evnt); }
                        else if (curr_evnt is DamageEvent) { _VFR_Damage_Friendly.Add(curr_evnt); }
                        else { _VFR_Other_CombatEvents.Add(curr_evnt); }
                    }
                }
            }

            bool show_debug_lines = false /*(Friendly_Name == "PlaguedSmilodonSummon")*/;
            if (show_debug_lines) { System.Diagnostics.Debug.WriteLine("---"); }
            
            if (_VFR_Other_CombatEvents.Count > (_VFR_Friendly_CombatEvents.Count + _VFR_Hostile_CombatEvents.Count))
            {
                if (show_debug_lines) System.Diagnostics.Debug.WriteLine("\tVFR: {0} isn't classified due to too many 'Other' events ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count);
                return 0;
            } // This shouldn't happen often, if ever, but if it does then the vote is inconclusive.
            else if (_VFR_Friendly_CombatEvents.Count > _VFR_Hostile_CombatEvents.Count)
            {
                if (show_debug_lines) System.Diagnostics.Debug.WriteLine("\tVFR: {0} appears to be Friendly ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count);
                changed_cnt = Set_Character_Type(CombatEvent.Char_Enum.Friendly);
            }
            else if (_VFR_Hostile_CombatEvents.Count > _VFR_Friendly_CombatEvents.Count)
            {
                if (show_debug_lines) System.Diagnostics.Debug.WriteLine("\tVFR: {0} appears to be Hostile ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count);
                changed_cnt = Set_Character_Type(CombatEvent.Char_Enum.Hostile);
            }
            else
            {
                string str = string.Format("\tVFR: {0} tie ({1} Friendly == {2} Hostile, {3} Other)", Source_Character_Name, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count);

                if (_VFR_Damage_Hostile.Count > _VFR_Damage_Friendly.Count)
                {
                    if (show_debug_lines) System.Diagnostics.Debug.WriteLine("{0}, assigned hostile based on damage ({1} Friendly < {2} Hostile).", str, _VFR_Damage_Friendly.Count, _VFR_Damage_Hostile.Count);
                    changed_cnt = Set_Character_Type(CombatEvent.Char_Enum.Hostile);
                }
                else if (_VFR_Damage_Friendly.Count > _VFR_Damage_Hostile.Count)
                {
                    if (show_debug_lines) System.Diagnostics.Debug.WriteLine("{0}, assigned friendly based on damage ({1} Friendly > {2} Hostile).", str, _VFR_Damage_Friendly.Count, _VFR_Damage_Hostile.Count);
                    changed_cnt = Set_Character_Type(CombatEvent.Char_Enum.Friendly);
                }
                else
                {
                    if (show_debug_lines) System.Diagnostics.Debug.WriteLine("{0}, still tie after considering damage ({1} Friendly == {2} Hostile).", str, _VFR_Damage_Friendly.Count, _VFR_Damage_Hostile.Count);
                }
            }

            //System.Diagnostics.Debug.WriteLine("Updated {0} combat events", changed_cnt);

            return changed_cnt;
        }

        /// <summary>
        /// Creates a new CharacterListItem from a single CombatEvent ("inParent").
        /// </summary>
        /// <param name="inParent"></param>
        public CharacterListItem(CombatEvent inParent)
        {
            AddParent(inParent);
        }

        /// <summary>
        /// Add a new CombatEvent to the "Parents" CombatEventList.  If the new CombatEvent is newer (has a lower ID) than the current CombatEvent stored in _Parent, it updates _Parent
        /// to the new value -- this makes it feasible to sort a group of CharacterListItems in chronlogical order "by first appearance".
        /// 
        /// This does *not* attempt to verify that the new CombatEvent is consistant with the existing CombatEvents attached to this CharacterListItem -- specifically, it doesn't check that
        /// either the Source_Character_Name or Source_Target_Character_Name (for CombatEventTargeted) matches the other CombatEvents already attached.  Perhaps it should, because I'm already
        /// iterating over the Parents CombatEventList, but it doesn't.
        /// 
        /// It *DOES* check for straight-up duplicates (adding the same CombatEvent more than once), and silently ignores attempts to add such CombatEvents.
        /// </summary>
        /// <param name="inParent">The CombatEvent to be added</param>
        public void AddParent(CombatEvent inParent)
        {
            if (_Parent == null) { _Parent = inParent; }
            if (inParent.ID < _Parent.ID) { _Parent = inParent; } // This allows sorting the list in chronological order by first appearance

            foreach (CombatEvent curr_event in _Parents) { if (curr_event == inParent) { return; } } // Ignore dups.

            _Parents.Add(inParent);
            inParent.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged);
        }

        /// <summary>
        /// Adds all of the events in a CombatEventList to this CharacterListItem to the Parents collection.
        /// </summary>
        /// <param name="inParents">The CombatEventList containing the CombatEvents to be added</param>
        public void AddParents(CombatEventList inParents)
        {
            if (inParents.Count != 0) { foreach (CombatEvent curr_event in inParents) { AddParent(curr_event); } }
        }

        /// <summary>
        /// Event fires if any of the *direct* children of this CharacterListItem are modified in certain ways.  Specifically, this event is raised if the Friendly_Name, Character_Name, 
        /// Character_Type, Target_Friendly_Name, Target_Character_Name, or Target_Character_Type values are altered.
        /// 
        /// The only current purpose of this event is to bubble change events up to the CharacterList (and, from there, up to the CombatEventContainer that contains the ChracterList) to
        /// force the rebuilding of *all* the CharacterLists.
        /// 
        /// When writting this comment, I realized that changes to Friendly_Name may result in an invalid CharacterList (where not all Parents have the right Friendly_Name to belong in
        /// the CharacterList), and this may result in bugs.
        /// </summary>
        /// <param name="source">The CombatEvent that changed</param>
        private void CombatEventChanged(CombatEvent source)
        {
            OnCombatEventChanged?.Invoke(source);
        }

        /// <summary>
        /// I override CompareTo so that the default operation .Sort() operation on CharacterList is to sort by Friendly_Name, alphabetically.
        /// </summary>
        /// <param name="other">The CombatEvent which is being compared with</param>
        /// <returns></returns>
        public int CompareTo(object other) // Used to sort the list
        {
            if (other is CharacterListItem)
            {
                CharacterListItem tmp_other = (CharacterListItem)other;
                return string.Compare(this.Friendly_Name, tmp_other.Friendly_Name);
            }
            else { throw new System.Exception("Attempted to compare CharacterListItem with '" + other.GetType().ToString() + "', which is not supported."); }
        }

        private UserControl _Details_UserControl = null;
        private DockPanel _Details_DockPanel = null;
        private Grid _Details_OuterGrid = null;

        private CombatStats _Details_Stats = null;

        /// <summary>
        /// Creates and populates a UserControl suitable for displaying the details of a CharacterListItem.  This includes:
        ///   1) A "Summary" area, that contains general information about the CharacterListItem being viewed,
        ///   2) A "Statistics" area, (generated by CombatStats) that contains information on the CombatEvents that support this CharacterListItem,
        ///   3) A "Source" areas, that contains a WebBrowser control showing the concatenated Source_With_IDs attached to all the CombatEvents supporting this CharacterListItem.
        /// </summary>
        /// <param name="show_all">If true, the CombatEvents attached to the CharacterListItems in the "Children" list are included in the output.  Otherwise, only direct attached
        /// CombatEvents (ones from Parents) are included.
        /// </param>
        /// <returns>The UserControl (which may or may not be new)</returns>
        public UserControl Get_UserControl_For_Display(bool show_all = false)
        {
            // I'm fully aware that this method, along with similar methods attached to derivied types of CombatEvent, violates the WPF standard for seperating data and UI.

            // Further, I'm aware that in WPF the vast bulk of UI generation is meant to be handled via XAML, using ControlTemplate and Style tags to customize off the shelf UI
            // Controls as required for a specific application, with XAML defined UserControls handling situations that can't be handled via ControlTemplate and Style.

            // I *did* look, briefly, at DataBinding in WPF -- and even a very quick, very casual, inspection reveals that it is mostly intended to be used with data coming
            // from a database, via classes such as "DataSet", "DataTable" and similar.  In such an environement, the data being displayed corrsponds with rows and columns in a DataTable, 
            // and the DataTable provides standardized "OnUpdate" events that are bubbled to the bound UI elements to keep the UI in sync with the data.

            // That's great -- but none of my data is coming from a proper database, so I could easily imagine spending weeks of work trying to set up the inheritence and events properly to make
            // databinding work with arrays (from CombatStats) and Lists (for CombatEvents) and spend more time working out how to use XAML to describe the controls (hopefully, in a way that
            // allowed the controls to incorperate new data, if I decided to start outputing something new).  And all of this time would have to be invested *before* I could produce anything 
            // of use to anyone, including myself.

            // Or I could just create everything programatically (very little XAML) and manually handle populating and updating UI Controls.  The upside is that I could start producing usable
            // results almost immediately -- the downside is that I'm not using WPF "as intended".  Since this is very much a hobbiest project... :)

            if (_Details_UserControl == null)
            {
                _Details_UserControl = new UserControl();
                ScrollViewer scrollViewer = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
                _Details_UserControl.Content = scrollViewer;

                _Details_DockPanel = new DockPanel() { LastChildFill = false };
                scrollViewer.Content = _Details_DockPanel;

                _Details_OuterGrid = new Grid();
                DockPanel.SetDock(_Details_OuterGrid, Dock.Top);
                _Details_DockPanel.Children.Add(_Details_OuterGrid);

                _Details_OuterGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });
            }

            return Update_UserControl_For_Display(show_all);
        }

        /// <summary>
        /// Populates a UserControl suitable for displaying the details of this CharacterListItem.  This includes:
        ///   1) A "Summary" area, that contains general information about the CharacterListItem being viewed,
        ///   2) A "Statistics" area, (generated by CombatStats) that contains information on the CombatEvents that support this CharacterListItem,
        ///   3) A "Source" areas, that contains a WebBrowser control showing the concatenated Source_With_IDs attached to all the CombatEvents supporting this CharacterListItem.
        ///   
        /// If the UserControl has not already been created, this throws an exception.
        /// </summary>
        /// <param name="show_all">If true, the CombatEvents attached to the CharacterListItems in the "Children" list are included in the output.  Otherwise, only direct attached
        /// CombatEvents (ones from Parents) are included.
        /// </param>
        /// <returns>The populated UserControl</returns>
        public UserControl Update_UserControl_For_Display(bool show_all = false)
        {
            if (_Details_UserControl == null) { throw new System.Exception("In CharacterListItem.Update_UserControl_For_Display when _Details_UserControl == null."); }

            if (_Parent == null)
            { throw new System.Exception("In CharacterListItem.Update_UserControl_For_Display when _Parent == null"); }

            _Details_OuterGrid.RowDefinitions.Clear();
            _Details_OuterGrid.Children.Clear();

            // Adding items to this array will result in those items being displayed in the UserControl.  See how easy it is if you don't use XAML?

            // Yes, I'm aware that this method is wasteful of resources (instead of re-using UI controls, I detatch and recreate them) and isn't standards compliant, but it *is* easy.
            // Come to the dark side of standard violation -- we have cookies!

            string[,] char_info =
            {
                { "Friendly Character Name", Friendly_Name },
                { "Source Character Name", Source_Character_Name },
                { "Faction", String.Format("{0} (VFR: Friendly {1}, Hostile {2}, Don't Know {3})", Character_Type, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count) },
                { "Number of characters", (Children.Count+1).ToString() }
            };

            // I'm fully aware that you aren't supposed to do this (using a method from _Parent to create the table, when creating Window controls isn't something that you would expect
            // CombatEvent to do).

            // The correct solution would be:
            //   1) For this class to inherit from CombatEvent.  This would be great, and this class already implements many of the CombatEvent required properties and methods.  But...  I want to
            //      perserve the option for updating _Parent (which *is* a CombatEvent) so that the _Parent is always the chronolgically earliest CombatEvent that exists in this control.  And if I
            //      made *this* a CombatEvent, I'd lose that option.  Additionally, CombatEvents are meant to be created via parsing lines of text from combatLog.txt, but CharacterListItems are 
            //      created by feeding them already created CombatEvents.  Inheriting from CombatEvent would result in a signficant amount of methods that simply don't apply to CharacterListItem.
            //   2) Create a new class that both CombatEvent and this would inherit from that implements the required methods.  This would require quite a bit of refactoring, but it should work.
            //   3) Create a static "Helper" class that contains the required methods, and then both CombatEvent descendents and CharacterListItems could call the static methods.  This would require
            //      even more refactoring than #2.

            // All of those seem like overkill when all I need to do is create a very short, very narrow Windows Grid control containing 4 items.  Once, right here.  And I don't need the added
            // flexability that having this class have a common ancestor with CombatEvent would provide.  And its a hobbiest project.  And I'm already violating several "best pratices", so... 

            ScrollViewer tmp_char_info_scrollViewer = _Parent.New_Windows_Table("Summary", char_info, 2, 100);

            // This little dance is required because New_Windows_Table automatically wraps the Grid in a scrollViewer.  Which is great, if you have lots of columns, as it allows horzontal scrolling.
                
            // If you *don't* have lots of columns, and thus don't need horizontal scrolling, it results in the mouse wheel being captured by the (hidden and non-functional) scrollViewer, producing
            // a confusing user experience.

            // Basically, you want the scrollViewer when you don't expect to be displaying enough data to require routine vertical scrolling, but don't otherwise.  In this case, since we are displaying
            // both a Stats UserControl and a webBrowser, vertical scrolling is absolutely going to be required, and thus, we need to get rid of the scrollViewer.

            Grid char_info_Grid = ((Grid)tmp_char_info_scrollViewer.Content);
            tmp_char_info_scrollViewer.Content = null;

            _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            Grid.SetRow(char_info_Grid, _Details_OuterGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(char_info_Grid, 0);
            _Details_OuterGrid.Children.Add(char_info_Grid);

            // "all_events" is used to determine as a source for later controls.  This provides an easy method to switch between showing controls that account for CombatEvents attached to 
            // CharacterListItems in the Children collection and not.

            CombatEventList all_events = null;
            if (show_all) { all_events = Get_All_CombatEvents(); }
            else { all_events = _Parents; }

            TextBlock stats_Title = new TextBlock(new Run("Statistics") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline }) { HorizontalAlignment = HorizontalAlignment.Center };
            _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            Grid.SetRow(stats_Title, _Details_OuterGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(stats_Title, 0);
            _Details_OuterGrid.Children.Add(stats_Title);

            Run stats_Note_Run = new Run("Statistics include attacks and damage targeted at a character, not just attacks and damage inflicted by the character.");
            TextBlock stats_Note = new TextBlock(stats_Note_Run) { HorizontalAlignment = HorizontalAlignment.Left };
            _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            Grid.SetRow(stats_Note, _Details_OuterGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(stats_Note, 0);
            _Details_OuterGrid.Children.Add(stats_Note);

            if (_Details_Stats == null) { _Details_Stats = new CombatStats(); }
            if (_Details_Stats.CombatEvent_Count != all_events.Count) { _Details_Stats.Recalculate_Stats(all_events); }
            UserControl stats_uc = _Details_Stats.Get_Analysis_UserControl();
            if (stats_uc.Parent != null) // This happens when you click on the root tree node (which shows all events that fall under this friendlyname), then click on the detail that corrsponds with the parent.
            {
                ((Grid)stats_uc.Parent).Children.Remove(stats_uc);
            }
            _Details_Stats.Update_Analysis_UserControl();

            _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            Grid.SetRow(stats_uc, _Details_OuterGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(stats_uc, 0);
            _Details_OuterGrid.Children.Add(stats_uc);

            TextBlock source_Title = new TextBlock(new Run("Source") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline }) { HorizontalAlignment = HorizontalAlignment.Center };
            _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            Grid.SetRow(source_Title, _Details_OuterGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(source_Title, 0);
            _Details_OuterGrid.Children.Add(source_Title);

            // _Parent.New_WebBrowser() creates a WebBrowser that has an event attached the "OnLoadCompleted" event to run JavaScript to disable word-wrap (and, by implication, enable horizontal
            // scrolling).  This allows more CombatEvents to fit into a given amount of vertical space, and the left most portion of the text tends to be the only text the user would be interested
            // in anyway.

            WebBrowser webBrowser = _Parent.New_WebBrowser();

            _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(800, System.Windows.GridUnitType.Pixel) });
            Grid.SetRow(webBrowser, _Details_OuterGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(webBrowser, 0);
            Grid.SetColumnSpan(webBrowser, 2);
            _Details_OuterGrid.Children.Add(webBrowser);

            // This whole block should be wrapped in a method and put somewhere (its a copy & paste from CombatEventContainer).  But there is no good place to put it, and this is (currently)
            // the only places where it is re-used, so...

            Button refresh_button = new Button() { Content = new TextBlock(new Run("Refresh")), Width = 100, HorizontalAlignment = HorizontalAlignment.Right };
            refresh_button.Click +=
                (sender, obj) =>
                {
                    refresh_button.IsEnabled = false;
                    // It baffles me that a lambda function can access local variables (such as webBrowser) even when those variables would be far, far out of scope by the time
                    // the lambda function is executed.  Don't get me wrong -- its amazingly handy to be able to do this, but it seems wrong somehow -- like I'm "cheating the system".
                    webBrowser.NavigateToString(string.Format("Loading {0} events -- please be patient", Children.Count));
                    string tmp_string = "";

                    //Spin off generating the string to a background thread -- this allows the UI to update while the string is built.  This is overkill here (you really need 10k+ CombatEvents
                    //for it to make a big diffrerence), but its easy to implement (once you know how) and does slightly improve UI interactivity.
                    System.ComponentModel.BackgroundWorker bg = new System.ComponentModel.BackgroundWorker();
                    bg.DoWork += (bg_sender, bg_obj) => tmp_string = _Parent.Filter_String_For_WebBrowser(Source_With_ID);
                    bg.RunWorkerCompleted += (bg_sender, bg_obj) => { webBrowser.NavigateToString(tmp_string); refresh_button.IsEnabled = true; };
                    bg.RunWorkerAsync();
                };
            Grid.SetRow(refresh_button, _Details_OuterGrid.RowDefinitions.Count - 2); // Backup to the "Source" title row.
            Grid.SetColumn(refresh_button, 0);
            _Details_OuterGrid.Children.Add(refresh_button);

            // The threshold to require a manual refersh to populate the webBrowser could be increased with minimal impact on the user experience -- but it would make debugging harder, as
            // I'd have to find a larger dataset to trigger the requirement to manuall refresh.  And it *does* take signficant time to load even 1k events into the webBrowser.
            if (Children.Count < 1000) { webBrowser.NavigateToString(_Parent.Filter_String_For_WebBrowser(Source_With_ID)); }
            else { webBrowser.NavigateToString(string.Format("Refresh to view {0} events -- this may take a while to generate!", Children.Count)); }

            return _Details_UserControl;
        }

    }
}
