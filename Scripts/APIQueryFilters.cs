﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO
{
    // ---------[ BASE CLASSES ]---------
    public class Filter
    {
        public readonly static Filter None = new Filter();

        protected int limit = 20;

        public virtual string GenerateQueryString()
        {
            return "_limit=" + limit;
        }
    }

    public abstract class Filter<T,E> : Filter
    {
        internal delegate int SortDelegate(T a, T b);
        internal delegate T_Field GetFieldDelegate<T_Field>(T o);

        // ---------[ FIELD INFORMATION MAPPING ]---------
        internal class FieldInformation
        {
            public string apiFilterName;
            public SortDelegate sortAscendingDelegate;

            public GetFieldDelegate<bool> getFieldAsBoolean;
            public GetFieldDelegate<int> getFieldAsInt;
            public GetFieldDelegate<float> getFieldAsFloat;
            public GetFieldDelegate<string> getFieldAsString;
            public GetFieldDelegate<string[]> getFieldAsStringArray;

            public FieldInformation(string filterName)
            {
                this.apiFilterName = filterName;
            }
        }

        // ------[ VARIABLES ]------
        protected string sortString = "";
        protected Comparison<T> sortDelegate = null;

        protected delegate bool FieldFilterDelegate(T o);
        protected Dictionary<E, string> filterStringMap = new Dictionary<E, string>();
        protected Dictionary<E, FieldFilterDelegate> filterDelegateMap = new Dictionary<E, FieldFilterDelegate>();

        // ------[ INITIALIZATION ]---
        internal Filter(string initialSortString, SortDelegate initialSortDelegate)
        {
            Debug.Assert(!String.IsNullOrEmpty(initialSortString)
                         && initialSortDelegate != null);

            sortString = initialSortString;
            sortDelegate = (a,b) => initialSortDelegate(a,b);
        }

        public abstract void ResetSorting();
        internal abstract FieldInformation GetFieldInformation(E fieldIdentifier);

        // ------[ OUTPUT FUNCTIONS ]------
        public override string GenerateQueryString()
        {
            string filterString = "_limit=" + limit;
            filterString += "&_sort=" + sortString;

            foreach(string fs in filterStringMap.Values)
            {
                filterString += "&" + fs;
            }

            return filterString;
        }
        public T[] FilterCollection(ICollection<T> objectCollection)
        {
            List<T> filteredList = new List<T>(objectCollection.Count);

            foreach(T o in objectCollection)
            {
                bool doAdd = true;
                foreach(FieldFilterDelegate isObjectAccepted in filterDelegateMap.Values)
                {
                    if(!isObjectAccepted(o))
                    {
                        doAdd = false;
                        break;
                    }
                }

                if(doAdd)
                {
                    filteredList.Add(o);
                }
            }

            filteredList.Sort(sortDelegate);

            T[] retVal;

            if(filteredList.Count <= limit)
            {
                retVal = filteredList.ToArray();
            }
            else
            {
                retVal = new T[limit];
                filteredList.CopyTo(0, retVal, 0, limit);
            }

            return retVal;
        }

        // ------[ SORTING APPLICATION ]------
        public virtual void ApplySortAscending(E fieldIdentifier) 
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);
            sortString = info.apiFilterName;
            sortDelegate = (a,b) => info.sortAscendingDelegate(a,b);
        }
        public virtual void ApplySortDescending(E fieldIdentifier) 
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);
            sortString = "-" + info.apiFilterName;
            sortDelegate = (a,b) => info.sortAscendingDelegate(b,a);
        }

        // ------[ FILTER APPLICATION ]------
        public void ClearOnField(E fieldIdentifier) 
        {
            filterStringMap.Remove(fieldIdentifier);
            filterDelegateMap.Remove(fieldIdentifier);
        }

        // ---[ BOOLEAN FILTERS ]---
        public void ApplyBooleanIs(E fieldIdentifier, bool value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value.ToString();
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsBoolean(o) == value; };
        }
        public void ApplyBooleanIsNot(E fieldIdentifier, bool value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not=" + value.ToString();
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsBoolean(o) != value; };
        }

        // ---[ INTEGER FILTERS ]---
        public void ApplyIntEquality(E fieldIdentifier, int value) 
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) == value; };
        }
        public void ApplyIntInequality(E fieldIdentifier, int value) 
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) != value; };
        }
        public void ApplyIntInArray(E fieldIdentifier, 
                                    int[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0].ToString();
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return values.Contains(info.getFieldAsInt(o)); };
        }
        public void ApplyIntNotInArray(E fieldIdentifier, 
                                       int[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0].ToString();
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return !values.Contains(info.getFieldAsInt(o)); };
        }
        public void ApplyIntMinimum(E fieldIdentifier, 
                                    int value, bool isValueInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            if(isValueInclusive)
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-min=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) >= value; };
            }
            else
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-gt=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) > value; };
            }
        }
        public void ApplyIntMaximum(E fieldIdentifier, 
                                    int value, bool isValueInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            if(isValueInclusive)
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-max=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) <= value; };
            }
            else
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-lt=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) < value; };
            }
        }
        public void ApplyIntRange(E fieldIdentifier, 
                                  int minimum, bool isMinimumInclusive, 
                                  int maximum, bool isMaximumInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string minString;
            string maxString;
            FieldFilterDelegate minDelegate;
            FieldFilterDelegate maxDelegate;

            if(isMinimumInclusive)
            {
                minString = info.apiFilterName + "-min=" + minimum;
                minDelegate = (o) => { return info.getFieldAsInt(o) >= minimum; };
            }
            else
            {
                minString = info.apiFilterName + "-gt=" + minimum;
                minDelegate = (o) => { return info.getFieldAsInt(o) > minimum; };
            }

            if(isMaximumInclusive)
            {
                maxString = info.apiFilterName + "-max=" + maximum;
                maxDelegate = (o) => { return info.getFieldAsInt(o) <= maximum; };
            }
            else
            {
                maxString = info.apiFilterName + "-lt=" + maximum;
                maxDelegate = (o) => { return info.getFieldAsInt(o) < maximum; };
            }

            filterStringMap[fieldIdentifier] = minString + "&" + maxString;
            filterDelegateMap[fieldIdentifier] = (o) => { return minDelegate(o) && maxDelegate(o); };
        }
        public void ApplyBitwiseAnd(E fieldIdentifier, int value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-bitwise-and=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return (info.getFieldAsInt(o) & value) == value; };
        }

        // ---[ FLOAT FILTERS ]---
        public void ApplyFloatEquality(E fieldIdentifier, float value) 
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) == value; };
        }
        public void ApplyFloatInequality(E fieldIdentifier, float value) 
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) != value; };
        }
        public void ApplyFloatInArray(E fieldIdentifier, 
                                      float[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0].ToString();
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return values.Contains(info.getFieldAsFloat(o)); };
        }
        public void ApplyFloatNotInArray(E fieldIdentifier, 
                                         float[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0].ToString();
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return !values.Contains(info.getFieldAsFloat(o)); };
        }
        public void ApplyFloatMinimum(E fieldIdentifier, 
                                      float value, bool isValueInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            if(isValueInclusive)
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-min=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) >= value; };
            }
            else
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-gt=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) > value; };
            }
        }
        public void ApplyFloatMaximum(E fieldIdentifier, 
                                      float value, bool isValueInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            if(isValueInclusive)
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-max=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) <= value; };
            }
            else
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-lt=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) < value; };
            }
        }
        public void ApplyFloatRange(E fieldIdentifier, 
                                    float minimum, bool isMinimumInclusive, 
                                    float maximum, bool isMaximumInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string minString;
            string maxString;
            FieldFilterDelegate minDelegate;
            FieldFilterDelegate maxDelegate;

            if(isMinimumInclusive)
            {
                minString = info.apiFilterName + "-min=" + minimum;
                minDelegate = (o) => { return info.getFieldAsFloat(o) >= minimum; };
            }
            else
            {
                minString = info.apiFilterName + "-gt=" + minimum;
                minDelegate = (o) => { return info.getFieldAsFloat(o) > minimum; };
            }

            if(isMaximumInclusive)
            {
                maxString = info.apiFilterName + "-max=" + maximum;
                maxDelegate = (o) => { return info.getFieldAsFloat(o) <= maximum; };
            }
            else
            {
                maxString = info.apiFilterName + "-lt=" + maximum;
                maxDelegate = (o) => { return info.getFieldAsFloat(o) < maximum; };
            }

            filterStringMap[fieldIdentifier] = minString + "&" + maxString;
            filterDelegateMap[fieldIdentifier] = (o) => { return minDelegate(o) && maxDelegate(o); };
        }

        // ---[ STRING FILTERS ]---
        public void ApplyStringEquality(E fieldIdentifier, 
                                        string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsString(o) == value; };
        }
        public void ApplyStringInequality(E fieldIdentifier, 
                                          string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsString(o) != value; };
        }
        public void ApplyStringInArray(E fieldIdentifier, 
                                       string[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0];
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return values.Contains(info.getFieldAsString(o)); };
        }
        public void ApplyStringNotInArray(E fieldIdentifier, 
                                          string[] values)
        {
            Debug.Assert(values.Length > 0);
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0];
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return !values.Contains(info.getFieldAsString(o)); };
        }

        public void ApplyStringLike(E fieldIdentifier, 
                                    string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-lk=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsString(o).Like(value.Replace('*', '%')); };
        }
        public void ApplyStringNotLike(E fieldIdentifier, 
                                       string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not-lk=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return !info.getFieldAsString(o).Like(value.Replace('*', '%')); };
        }

        // ---[ Array FILTERS ]---
        public void ApplyStringArrayContains(E fieldIdentifier, 
                                             string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsStringArray(o).Contains(value); };
        }
        public void ApplyStringArrayContainsAll(E fieldIdentifier, 
                                                string[] values)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            Debug.Assert(values.Length > 0);
            string filterString = values[0];
            for(int i = 1; // first tag is already handled
                i < values.Length;
                ++i)
            {
                filterString += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + filterString;

            filterDelegateMap[fieldIdentifier] = (T o) =>
            {
                List<string> unmatchedValues = new List<string>(values);
                foreach(string fieldValue in info.getFieldAsStringArray(o))
                {
                    unmatchedValues.Remove(fieldValue);
                }
                return unmatchedValues.Count == 0;
            };
        }        


        // ---[ HELPER FUNCTIONS FOR FIELD INFORMATION MAP GENERATION ]---
        internal static void StoreBooleanField(Dictionary<E, Filter<T,E>.FieldInformation> fieldInformationMap, 
                                               E fieldIdentifier, string apiFilterName, 
                                               Filter<T,E>.GetFieldDelegate<bool> getFieldAsBool, 
                                               Filter<T,E>.SortDelegate sortFieldAscending)
        {
            Filter<T,E>.FieldInformation info = new Filter<T,E>.FieldInformation(apiFilterName);
            info.getFieldAsBoolean = getFieldAsBool;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }

        internal static void StoreIntField(Dictionary<E, Filter<T,E>.FieldInformation> fieldInformationMap, 
                                           E fieldIdentifier, string apiFilterName, 
                                           Filter<T,E>.GetFieldDelegate<int> getFieldAsInt, 
                                           Filter<T,E>.SortDelegate sortFieldAscending)
        {
            Filter<T,E>.FieldInformation info = new Filter<T,E>.FieldInformation(apiFilterName);
            info.getFieldAsInt = getFieldAsInt;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }
        
        internal static void StoreFloatField(Dictionary<E, Filter<T,E>.FieldInformation> fieldInformationMap, 
                                             E fieldIdentifier, string apiFilterName, 
                                             Filter<T,E>.GetFieldDelegate<float> getFieldAsFloat, 
                                             Filter<T,E>.SortDelegate sortFieldAscending)
        {
            Filter<T,E>.FieldInformation info = new Filter<T,E>.FieldInformation(apiFilterName);
            info.getFieldAsFloat = getFieldAsFloat;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }
        
        internal static void StoreStringField(Dictionary<E, Filter<T,E>.FieldInformation> fieldInformationMap, 
                                              E fieldIdentifier, string apiFilterName, 
                                              Filter<T,E>.GetFieldDelegate<string> getFieldAsString, 
                                              Filter<T,E>.SortDelegate sortFieldAscending)
        {
            Filter<T,E>.FieldInformation info = new Filter<T,E>.FieldInformation(apiFilterName);
            info.getFieldAsString = getFieldAsString;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }
        
        internal static void StoreStringArrayField(Dictionary<E, Filter<T,E>.FieldInformation> fieldInformationMap, 
                                                   E fieldIdentifier, string apiFilterName, 
                                                   Filter<T,E>.GetFieldDelegate<string[]> getFieldAsStringArray, 
                                                   Filter<T,E>.SortDelegate sortFieldAscending)
        {
            Filter<T,E>.FieldInformation info = new Filter<T,E>.FieldInformation(apiFilterName);
            info.getFieldAsStringArray = getFieldAsStringArray;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }
    }

    // ---------[ QUERY FILTERS ]---------
    public class GetAllGamesFilter : Filter<Game, GetAllGamesFilter.Field>
    {
        public static readonly new GetAllGamesFilter None = new GetAllGamesFilter();

        // ---------[ FIELD MAPPING ]---------
        public enum Field
        {
            // integer Unique id of the game.
            ID, 
            // integer Status of the game (only admins can filter by this field, see status and visibility for details):
            Status, 
            // integer Unique id of the user who has ownership of the game.
            SubmittedBy, 
            // integer Unix timestamp of date game was registered.
            DateAdded, 
            // integer Unix timestamp of date game was updated.
            DateUpdated, 
            // integer Unix timestamp of date game was set live.
            DateLive, 
            // string  Name of the game.
            Name, 
            // string  Subdomain for the game on mod.io.
            NameID, 
            // string  Summary of the game.
            Summary, 
            // string  Official homepage of the game.
            Homepage, 
            // string  Word used to describe user-generated content (mods, items, addons etc).
            UGCName, 
            // integer Presentation style used on the mod.io website:
            PresentationOption, 
            // integer Submission process modders must follow
            SubmissionOption, 
            // integer Curation process used to approve mods
            CurationOption,
            // integer Community features enabled on the mod.io website:
            CommunityOptions, 
            // integer Revenue capabilities mods can enable
            RevenueOptions, 
            // integer Level of API access allowed by this game
            APIAccessOptions,
        }

        private static Dictionary<Field, FieldInformation> fieldInformationMap;

        static GetAllGamesFilter()
        {
            fieldInformationMap = new Dictionary<Field, FieldInformation>(Enum.GetNames(typeof(Field)).Length);

            StoreIntField(fieldInformationMap,
                          Field.ID, "id",
                          (game) => game.ID,
                          (a,b) => a.ID.CompareTo(b.ID));
            StoreIntField(fieldInformationMap,
                          Field.Status, "status", 
                          (game) => (int)game.GetStatus(),
                          (a,b) => a.GetStatus().CompareTo(b.GetStatus()));
            StoreIntField(fieldInformationMap,
                          Field.SubmittedBy, "submitted_by",
                          (game) => game.submittedBy.ID,
                          (a,b) => a.submittedBy.ID.CompareTo(b.submittedBy.ID));
            StoreIntField(fieldInformationMap,
                          Field.DateAdded, "date_added",
                          (game) => game.GetDateAdded().AsServerTimeStamp(),
                          (a,b) => a.GetDateAdded().AsServerTimeStamp().CompareTo(b.GetDateAdded().AsServerTimeStamp()));
            StoreIntField(fieldInformationMap,
                          Field.DateUpdated, "date_updated",
                          (game) => game.GetDateUpdated().AsServerTimeStamp(),
                          (a,b) => a.GetDateUpdated().AsServerTimeStamp().CompareTo(b.GetDateUpdated().AsServerTimeStamp()));
            StoreIntField(fieldInformationMap,
                          Field.DateLive, "date_live",
                          (game) => game.GetDateLive().AsServerTimeStamp(),
                          (a,b) => a.GetDateLive().AsServerTimeStamp().CompareTo(b.GetDateLive().AsServerTimeStamp()));
            StoreStringField(fieldInformationMap,
                             Field.Name, "name",
                             (game) => game.name,
                             (a,b) => a.name.CompareTo(b.name));
            StoreStringField(fieldInformationMap,
                             Field.NameID, "name_id",
                             (game) => game.nameID,
                             (a,b) => a.nameID.CompareTo(b.nameID));
            StoreStringField(fieldInformationMap,
                             Field.Summary, "summary",
                             (game) => game.summary,
                             (a,b) => a.summary.CompareTo(b.summary));
            StoreStringField(fieldInformationMap,
                             Field.Homepage, "homepage",
                             (game) => game.homepage,
                             (a,b) => a.homepage.CompareTo(b.homepage));
            StoreStringField(fieldInformationMap,
                             Field.UGCName, "ugc_name",
                             (game) => game.UGCName,
                             (a,b) => a.UGCName.CompareTo(b.UGCName));
            StoreIntField(fieldInformationMap,
                          Field.PresentationOption, "presentation_options",
                          (game) => (int)game.GetPresentationOption(),
                          (a,b) => a.GetPresentationOption().CompareTo(b.GetPresentationOption()));
            StoreIntField(fieldInformationMap,
                          Field.SubmissionOption, "submission_options",
                          (game) => (int)game.GetSubmissionOption(),
                          (a,b) => a.GetSubmissionOption().CompareTo(b.GetSubmissionOption()));
            StoreIntField(fieldInformationMap,
                          Field.CurationOption, "curation_options",
                          (game) => (int)game.GetCurationOption(),
                          (a,b) => a.GetCurationOption().CompareTo(b.GetCurationOption()));
            StoreIntField(fieldInformationMap,
                          Field.CommunityOptions, "community_options",
                          (game) => (int)game.GetCommunityOptions(),
                          (a,b) => a.GetCommunityOptions().CompareTo(b.GetCommunityOptions()));
            StoreIntField(fieldInformationMap,
                          Field.RevenueOptions, "revenue_options",
                          (game) => (int)game.GetRevenueOptions(),
                          (a,b) => a.GetRevenueOptions().CompareTo(b.GetRevenueOptions()));
            StoreIntField(fieldInformationMap,
                          Field.APIAccessOptions, "api_access_options",
                          (game) => (int)game.GetAPIAccessOptions(),
                          (a,b) => a.GetAPIAccessOptions().CompareTo(b.GetAPIAccessOptions()));
        }

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllGamesFilter() : base("id", (a,b) => a.ID.CompareTo(b.ID))
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(GetAllGamesFilter.Field fieldIdentifier)
        { 
            return GetAllGamesFilter.fieldInformationMap[fieldIdentifier]; 
        }

        // ---[ SPECIALIZED FILTERS ]---
        public void ApplyStatus(Game.Status value)
        {
            base.ApplyIntEquality(Field.Status, (int)value);
        }

        public void ApplyPresentation(Game.PresentationOption value)
        {
            base.ApplyIntEquality(Field.PresentationOption, (int)value);
        }

        public void ApplyModSubmissionMode(Game.SubmissionOption value)
        {
            base.ApplyIntEquality(Field.SubmissionOption, (int)value);
        }

        public void ApplyCurationMode(Game.CurationOption value)
        {
            base.ApplyIntEquality(Field.CurationOption, (int)value);
        }

        public void ApplyCommunityOptions(Game.CommunityOptions value)
        {
            base.ApplyIntEquality(Field.CommunityOptions, (int)value);
        }

        public void ApplyRevenueOptions(Game.RevenueOptions value)
        {
            base.ApplyIntEquality(Field.RevenueOptions, (int)value);
        }

        public void ApplyAPIAccessOptions(Game.APIAccessOptions value)
        {
            base.ApplyIntEquality(Field.APIAccessOptions, (int)value);
        }
    }

    // TODO(@jackson): REDO
    public class GetAllModsFilter : Filter<Mod, GetAllModsFilter.Field>
    {
        public enum Field
        {
            ID,
            GameID,
            Status,
            Visible,
            SubmittedBy,
            DateAdded,
            DateUpdated,
            DateLive,
            Logo,
            Homepage,
            Name,
            NameID,
            Summary,
            Description,
            MetadataBlob,
            MetadataKVP, //TODO(@jackson): IMPLEMENT
            Modfile,
            Price,
            Tags,
            Downloads,
            Popularity,
            Ratings,
            Subscribers,
        }

        public readonly static new GetAllModsFilter None = new GetAllModsFilter();

        // ---------[ FIELD MAPPING ]---------
        private static Dictionary<Field, FieldInformation> fieldInformationMap;

        static GetAllModsFilter()
        {
            fieldInformationMap = new Dictionary<Field, FieldInformation>(Enum.GetNames(typeof(Field)).Length);

            // integer(int32)  Unique id of the mod.
            StoreIntField(fieldInformationMap, 
                          Field.ID, "id", 
                          (mod) => mod.ID, 
                          (a,b) => a.ID.CompareTo(b.ID));
            // integer(int32)  Unique id of the parent game.
            StoreIntField(fieldInformationMap, 
                          Field.GameID, "game_id", 
                          (mod) => mod.gameID, 
                          (a,b) => a.gameID.CompareTo(b.gameID));
            // integer  Status of the mod (only game admins can filter by this field, see status and visibility for details)
            StoreIntField(fieldInformationMap, 
                          Field.Status, "status", 
                          (mod) => (int)mod.GetStatus(),
                          (a,b) => a.GetStatus().CompareTo(b.GetStatus()));
            // visible  integer Visibility of the mod (only game admins can filter by this field, see status and visibility for details)
            StoreIntField(fieldInformationMap, 
                          Field.Visible, "visible", 
                          (mod) => (int)mod.GetVisible(),
                          (a,b) => a.GetVisible().CompareTo(b.GetVisible()));

            // integer(int32)  Unique id of the user who has ownership of the game.
            StoreIntField(fieldInformationMap, 
                          Field.SubmittedBy, "submitted_by", 
                          (mod) => mod.submittedBy.ID, 
                          (a,b) => a.submittedBy.ID.CompareTo(b.submittedBy.ID));
            // integer(int32)  Unix timestamp of date registered.
            StoreIntField(fieldInformationMap, 
                          Field.DateAdded, "date_added", 
                          (mod) => mod.GetDateAdded().AsServerTimeStamp(),
                          (a,b) => a.GetDateAdded().AsServerTimeStamp().CompareTo(b.GetDateAdded().AsServerTimeStamp()));
            // integer(int32)  Unix timestamp of date updated.
            StoreIntField(fieldInformationMap, 
                          Field.DateUpdated, "date_updated", 
                          (mod) => mod.GetDateUpdated().AsServerTimeStamp(),
                          (a,b) => a.GetDateUpdated().AsServerTimeStamp().CompareTo(b.GetDateUpdated().AsServerTimeStamp()));
            // integer(int32)  Unix timestamp of date mod was set live.
            StoreIntField(fieldInformationMap, 
                          Field.DateLive, "date_live", 
                          (mod) => mod.GetDateLive().AsServerTimeStamp(),
                          (a,b) => a.GetDateLive().AsServerTimeStamp().CompareTo(b.GetDateLive().AsServerTimeStamp()));

            // string  The filename of the logo.
            StoreStringField(fieldInformationMap,
                             Field.Logo, "logo", 
                             (mod) => mod.logo.filename, 
                             (a,b) => a.logo.filename.CompareTo(b.logo.filename));
            // string  Official homepage of the mod.
            StoreStringField(fieldInformationMap,
                             Field.Homepage, "homepage", 
                             (mod) => mod.homepage, 
                             (a,b) => a.homepage.CompareTo(b.homepage));
            // string  Name of the mod.
            StoreStringField(fieldInformationMap,
                             Field.Name, "name", 
                             (mod) => mod.name, 
                             (a,b) => a.name.CompareTo(b.name));
            // string  The unique SEO friendly URL for your game.
            StoreStringField(fieldInformationMap,
                             Field.NameID, "name_id", 
                             (mod) => mod.nameID, 
                             (a,b) => a.nameID.CompareTo(b.nameID));
            // string  Summary of the mod.
            StoreStringField(fieldInformationMap,
                             Field.Summary, "summary", 
                             (mod) => mod.summary, 
                             (a,b) => a.summary.CompareTo(b.summary));
            // string  An extension of the summary. HTML Supported.
            StoreStringField(fieldInformationMap,
                             Field.Description, "description", 
                             (mod) => mod.description, 
                             (a,b) => a.description.CompareTo(b.description));
            // string  Comma-separated list of metadata words.
            StoreStringField(fieldInformationMap,
                             Field.MetadataBlob, "metadata_blob", 
                             (mod) => mod.metadataBlob, 
                             (a,b) => a.metadataBlob.CompareTo(b.metadataBlob));
            // integer(int32)  Unique id of the Modfile Object marked as current release.
            StoreIntField(fieldInformationMap, 
                          Field.Modfile, "modfile", 
                          (mod) => mod.modfile.ID, 
                          (a,b) => a.modfile.ID.CompareTo(b.modfile.ID));
            // string  Sort results by weighted rating using _sort filter, value should be ratings for descending or -ratings for ascending results.
            StoreFloatField(fieldInformationMap,
                            Field.Ratings, "ratings", 
                            (mod) => mod.ratingSummary.weightedAggregate, 
                            (a,b) => a.ratingSummary.weightedAggregate.CompareTo(b.ratingSummary.weightedAggregate));

            
            // --- Currently unable to be filtered/sorted locally ---
            // string  Sort results by most subscribers using _sort filter, value should be subscribers for descending or -subscribers for ascending results.
            StoreStringField(fieldInformationMap,
                             Field.Subscribers, "subscribers", 
                             (mod) => { Debug.LogError("Filtering on subscribers locally is currently not implemented"); return ""; }, 
                             (a,b) => { Debug.LogWarning("Sorting on subscribers locally is currently not implemented"); return a.ID.CompareTo(b.ID); });
            // string  Sort results by most downloads using _sort filter parameter, value should be downloads for descending or -downloads for ascending results.
            StoreStringField(fieldInformationMap,
                             Field.Downloads, "downloads", 
                             (mod) => { Debug.LogError("Filtering on downloads locally is currently not implemented"); return ""; }, 
                             (a,b) => { Debug.LogWarning("Sorting on downloads locally is currently not implemented"); return a.ID.CompareTo(b.ID); });
            // string  Sort results by popularity using _sort filter, value should be popular for descending or -popular for ascending results.
            StoreStringField(fieldInformationMap,
                             Field.Popularity, "popular",  
                             (mod) => { Debug.LogError("Filtering on popularity locally is currently not implemented"); return ""; }, 
                             (a,b) => { Debug.LogWarning("Sorting on popularity locally is currently not implemented"); return a.ID.CompareTo(b.ID); });


            // string  Comma-separated values representing the tags you want to filter the results by.
            //      Only tags that are supported by the parent game can be applied.
            //      To determine what tags are eligible, see the tags values within 'Tag Options' column on the parent Game Object.
            StoreStringArrayField(fieldInformationMap,
                                  Field.Tags, "tags", 
                                  (mod) => mod.GetTagNames(), 
                                  (a,b) => { Debug.LogError("The 'tags' attribute cannot be sorted on"); return a.ID.CompareTo(b.ID); });
        }

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModsFilter() : base("id", (a,b) => a.ID.CompareTo(b.ID))
        {
        }
        public override void ResetSorting()
        {
            ApplySortAscending(Field.ID);
        }
        internal override FieldInformation GetFieldInformation(GetAllModsFilter.Field fieldIdentifier)
        {
            return GetAllModsFilter.fieldInformationMap[fieldIdentifier];
        }

        // ---[ SPECIALIZED FILTERS ]---
        public void ApplyNameQuery(string query)
        {
            filterStringMap[Field.Name] = "_q=" + query;
            filterDelegateMap[Field.Name] = (mod => mod.name.Contains(query));
        }
        public void ApplyStatus(Mod.Status value)
        {
            base.ApplyIntEquality(Field.Status, (int)value);
        }
        public void ApplyVisible(Mod.Visibility value)
        {
            base.ApplyIntEquality(Field.Visible, (int)value);
        }
        public void ApplyKVP(MetadataKVP value)
        {
            Debug.LogError("Not yet implemented");

            // TODO(@jackson): FIX THIS! Won't work locally
            // ApplyStringEquality(Field.MetadataKVP, kvp.GetFilterString());
        }
        public void ApplyKVPArray(MetadataKVP[] value)
        {
            Debug.LogError("Not yet implemented");

            // TODO(@jackson): FIX THIS! Won't work locally
            // string[] filterStrings = new string[kvpArray];
            // for(int i = 0; i < kvpArray.Length; ++i)
            // {
            //     filterStrings[i] = kvpArray[i].GetFilterString();
            // }
            // ApplyStringArrayContainsAll(Field.MetadataKVP, filterStrings);
        }
    }

    public class GetAllModfilesFilter : Filter<Modfile, GetAllModfilesFilter.Field>
    {
        public enum Field
        {
            ID,
        }

        public static readonly new GetAllModfilesFilter None = new GetAllModfilesFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModfilesFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(GetAllModfilesFilter.Field fieldIdentifier) { return null; }
    }

    public class GetAllModEventsFilter : Filter<ModEvent, GetAllModEventsFilter.Field>
    {
        public static readonly new GetAllModEventsFilter None = new GetAllModEventsFilter();

        // ---------[ FIELD MAPPING ]---------
        public enum Field
        {
            // Unique id of the activity object.
            ID,
            // Unique id of the parent mod.
            ModID,
            // Unique id of the user who performed the action.
            UserID,
            // Unix timestamp of date mod was updated.
            DateAdded,
            // Type of change that occurred: MOD_UPDATE, MODFILE_UPDATE, MOD_VISIBILITY_CHANGE, MOD_LIVE
            EventType,
            // Returns only the latest unique events, which is useful for checking if the primary modfile has changed.
            // Default value is true. 
            Latest,
            // Returns only events connected to mods the authenticated user is subscribed to, which is useful for keeping the users mods up-to-date.
            // Default value is false.
            Subscribed,
        }

        private static Dictionary<Field, FieldInformation> fieldInformationMap;

        static GetAllModEventsFilter()
        {
            fieldInformationMap = new Dictionary<Field, FieldInformation>(Enum.GetNames(typeof(Field)).Length);

            StoreIntField(fieldInformationMap, 
                          Field.ID, "id",
                          (e) => e.ID,
                          (a,b) => a.ID.CompareTo(b.ID));
            StoreIntField(fieldInformationMap, 
                          Field.ModID, "mod_id",
                          (e) => e.modID,
                          (a,b) => a.modID.CompareTo(b.modID));
            StoreIntField(fieldInformationMap, 
                          Field.UserID, "user_id",
                          (e) => e.userID,
                          (a,b) => a.userID.CompareTo(b.userID));
            StoreIntField(fieldInformationMap,
                          Field.DateAdded, "date_added",
                          (e) => e.GetDateAdded().AsServerTimeStamp(),
                          (a,b) => a.GetDateAdded().AsServerTimeStamp().CompareTo(b.GetDateAdded().AsServerTimeStamp()));
            
            StoreStringField(fieldInformationMap,
                             Field.EventType, "event_type",
                             (e) => ModEvent.GetNameForType(e.GetEventType()),
                             (a,b) => a.GetEventType().CompareTo(b.GetEventType()));

            StoreBooleanField(fieldInformationMap,
                              Field.Latest, "latest",
                              (e) => { Debug.LogError("Filtering on latest locally is currently not implemented"); return false; }, 
                              (a,b) => { Debug.LogWarning("Sorting on latest locally is currently not implemented"); return a.ID.CompareTo(b.ID); });
            StoreBooleanField(fieldInformationMap,
                              Field.Subscribed, "subscribed",
                              (e) => { Debug.LogError("Filtering on subscribed locally is currently not implemented"); return false; }, 
                              (a,b) => { Debug.LogWarning("Sorting on subscribed locally is currently not implemented"); return a.ID.CompareTo(b.ID); });
        }

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModEventsFilter() : base("id", (a,b) => a.ID - b.ID)
        {

        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(GetAllModEventsFilter.Field fieldIdentifier)
        { 
            return GetAllModEventsFilter.fieldInformationMap[fieldIdentifier]; 
        }

        // ---[ SPECIALIZED FILTERS ]---
        public void ApplyEventType(ModEvent.EventType eventType)
        {
            base.ApplyStringEquality(Field.EventType, ModEvent.GetNameForType(eventType));
        }
    }

    public class GetModEventFilter : Filter<ModEvent, GetModEventFilter.Field>
    {
        public static readonly new GetModEventFilter None = new GetModEventFilter();

        // ---------[ FIELD MAPPING ]---------
        public enum Field
        {
            ID,
        }

        private static Dictionary<Field, FieldInformation> fieldInformationMap;

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetModEventFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }
        
        internal override FieldInformation GetFieldInformation(GetModEventFilter.Field fieldIdentifier) { return null; }
    }

    public class GetAllModTagsFilter : Filter<ModTag, GetAllModTagsFilter.Field>
    {
        public enum Field
        {
            ID,
        }

        public static readonly new GetAllModTagsFilter None = new GetAllModTagsFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModTagsFilter() : base("name", (a,b) => a.name.CompareTo(b.name))
        {
        }

        public override void ResetSorting()
        {
            sortString = "name";
            sortDelegate = (a,b) => { return a.name.CompareTo(b.name); };
        }

        internal override FieldInformation GetFieldInformation(GetAllModTagsFilter.Field fieldIdentifier) { return null; }
    }

    // Note(@jackson): The GetAllModKVPMetadata filter offers no fields

    public class GetAllModDependenciesFilter : Filter<ModDependency, GetAllModDependenciesFilter.Field>
    {
        public enum Field
        {
            ID,
        }

        public static readonly new GetAllModDependenciesFilter None = new GetAllModDependenciesFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModDependenciesFilter() : base("id", (a,b) => a.modID - b.modID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "mod_id";
            sortDelegate = (a,b) => { return a.modID.CompareTo(b.modID); };
        }

        internal override FieldInformation GetFieldInformation(GetAllModDependenciesFilter.Field fieldIdentifier) { return null; }
    }

    public class GetAllGameTeamMembersFilter : Filter<TeamMember, GetAllGameTeamMembersFilter.Field>
    {
        public enum Field
        {
            ID,
        }

        public static readonly new GetAllGameTeamMembersFilter None = new GetAllGameTeamMembersFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllGameTeamMembersFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(GetAllGameTeamMembersFilter.Field fieldIdentifier) { return null; }
    }

    public class GetAllModTeamMembersFilter : Filter<TeamMember, GetAllModTeamMembersFilter.Field>
    {
        public enum Field
        {
            ID,
        }

        public static readonly new GetAllModTeamMembersFilter None = new GetAllModTeamMembersFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModTeamMembersFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(GetAllModTeamMembersFilter.Field fieldIdentifier) { return null; }
    }

    public class GetAllModCommentsFilter : Filter<Comment, GetAllModCommentsFilter.Field>
    {
        public enum Field
        {
            ID,
        }

        public static readonly new GetAllModCommentsFilter None = new GetAllModCommentsFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModCommentsFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(GetAllModCommentsFilter.Field fieldIdentifier) { return null; }
    }

    public class GetAllUsersFilter : Filter<User, GetAllUsersFilter.Field>
    {
        public enum Field
        {
            ID,
        }

        public static readonly new GetAllUsersFilter None = new GetAllUsersFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllUsersFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(GetAllUsersFilter.Field fieldIdentifier) { return null; }
    }

    public class GetUserSubscriptionsFilter : Filter<Mod, GetUserSubscriptionsFilter.Field>
    {
        public static readonly new GetUserSubscriptionsFilter None = new GetUserSubscriptionsFilter();

        // ---------[ FIELD MAPPING ]---------
        public enum Field
        {
            // integer Unique id of the mod.
            ID, 
            // integer Unique id of the parent game.
            GameID, 
            // integer Unique id of the user who has ownership of the mod.
            SubmittedBy, 
            // integer Unix timestamp of date mod was registered.
            DateAdded, 
            // integer Unix timestamp of date mod was updated.
            DateUpdated, 
            // integer Unix timestamp of date mod was set live.
            DateLive, 
            // string  Name of the mod.
            Name, 
            // string  Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here
            NameID, 
            // string  Summary of the mod.
            Summary, 
            // string  Detailed description of the mod which allows HTML.
            Description, 
            // string  Official homepage of the mod.
            Homepage, 
            // string  Metadata stored by the game developer.
            MetadataBlob, 
            // string  Comma-separated values representing the tags you want to filter the results by. Only tags that are supported by the parent game can be applied. To determine what tags are eligible, see the tags values within tag_options column on the parent Game Object.
            Tags, 
            // string  Sort results by most downloads using _sort filter parameter, value should be downloads for descending or -downloads for ascending results.
            Downloads, 
            // string  Sort results by popularity using _sort filter, value should be popular for descending or -popular for ascending results.
            Popular, 
            // string  Sort results by weighted rating using _sort filter, value should be rating for descending or -rating for ascending results.
            Rating, 
            // string  Sort results by most subscribers using _sort filter, value should be subscribers for descending or -subscribers for ascending results.
            Subscribers, 
        }

        private static Dictionary<Field, FieldInformation> fieldInformationMap;

        static void GetAllGamesFilter()
        {
            fieldInformationMap = new Dictionary<Field, FieldInformation>(Enum.GetNames(typeof(Field)).Length);

            StoreIntField(fieldInformationMap,
                          Field.ID, "id",
                          (mod) => mod.ID,
                          (a,b) => a.ID.CompareTo(b.ID));
            StoreIntField(fieldInformationMap,
                          Field.GameID, "game_id",
                          (mod) => mod.gameID,
                          (a,b) => a.gameID.CompareTo(b.gameID));
            StoreIntField(fieldInformationMap,
                          Field.SubmittedBy, "submitted_by",
                          (mod) => mod.submittedBy.ID,
                          (a,b) => a.submittedBy.ID.CompareTo(b.submittedBy.ID));
            StoreIntField(fieldInformationMap,
                          Field.DateAdded, "date_added",
                          (mod) => mod.GetDateAdded().AsServerTimeStamp(),
                          (a,b) => a.GetDateAdded().AsServerTimeStamp().CompareTo(b.GetDateAdded().AsServerTimeStamp()));
            StoreIntField(fieldInformationMap,
                          Field.DateUpdated, "date_updated",
                          (mod) => mod.GetDateUpdated().AsServerTimeStamp(),
                          (a,b) => a.GetDateUpdated().AsServerTimeStamp().CompareTo(b.GetDateUpdated().AsServerTimeStamp()));
            StoreIntField(fieldInformationMap,
                          Field.DateLive, "date_live",
                          (mod) => mod.GetDateLive().AsServerTimeStamp(),
                          (a,b) => a.GetDateLive().AsServerTimeStamp().CompareTo(b.GetDateLive().AsServerTimeStamp()));
            StoreStringField(fieldInformationMap,
                             Field.Name, "name",
                             (mod) => mod.name,
                             (a,b) => a.name.CompareTo(b.name));
            StoreStringField(fieldInformationMap,
                             Field.NameID, "name_id",
                             (mod) => mod.nameID,
                             (a,b) => a.nameID.CompareTo(b.nameID));
            StoreStringField(fieldInformationMap,
                             Field.Summary, "summary",
                             (mod) => mod.summary,
                             (a,b) => a.summary.CompareTo(b.summary));
            StoreStringField(fieldInformationMap,
                             Field.Description, "description",
                             (mod) => mod.description,
                             (a,b) => a.description.CompareTo(b.description));
            StoreStringField(fieldInformationMap,
                             Field.Homepage, "homepage",
                             (mod) => mod.homepage,
                             (a,b) => a.homepage.CompareTo(b.homepage));
            StoreStringField(fieldInformationMap,
                             Field.MetadataBlob, "metadata_blob",
                             (mod) => mod.metadataBlob,
                             (a,b) => a.metadataBlob.CompareTo(b.metadataBlob));
            
            // StoreStringField(fieldInformationMap,
            //                  Field.Tags, "tags",
            //                  (mod) => mod.tags,
            //                  (a,b) => a.tags.CompareTo(b.tags));

            // These fields sort in reverse
            // StoreStringField(fieldInformationMap,
            //                  Field.Downloads, "downloads",
            //                  (mod) => mod.downloads,
            //                  (b,a) => a.downloads.CompareTo(b.downloads));
            // StoreStringField(fieldInformationMap,
            //                  Field.Popular, "popular",
            //                  (mod) => mod.popular,
            //                  (b,a) => a.popular.CompareTo(b.popular));
            // StoreStringField(fieldInformationMap,
            //                  Field.Rating, "rating",
            //                  (mod) => mod.rating,
            //                  (b,a) => a.rating.CompareTo(b.rating));
            // StoreStringField(fieldInformationMap,
            //                  Field.Subscribers, "subscribers",
            //                  (mod) => mod.subscribers,
            //                  (b,a) => a.subscribers.CompareTo(b.subscribers));
        }

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetUserSubscriptionsFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        public override string GenerateQueryString()
        {
            Debug.Assert(ModManager.APIClient != null
                         && ModManager.APIClient.gameID > 0,
                         "This filter cannot be used until the ModManager has been initialized and the APIClient given a valid Game ID");

            return base.GenerateQueryString() + "&game_id=" + ModManager.APIClient.gameID;
        }

        internal override FieldInformation GetFieldInformation(GetUserSubscriptionsFilter.Field fieldIdentifier)
        { 
            return GetUserSubscriptionsFilter.fieldInformationMap[fieldIdentifier];
        }
        
        // ---[ SPECIALIZED FILTERS ]---
    }
}