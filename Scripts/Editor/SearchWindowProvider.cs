using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace abc.unity.CustomEditors
{
    public class SearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        public Action<object> EntrySelected;

        private List<Type> _items;

        public void Construct(List<Type> items)
        {
            _items = items;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> list = new List<SearchTreeEntry>();
            List<string> groups = new List<string>();
            foreach (var item in _items)
            {
                var typeName = item.FullName;
                string[] entryTitle = typeName.Split('.');
                string groupName = "";

                for (int i = 0; i < entryTitle.Length - 1; i++)
                {
                    groupName += entryTitle[i];
                    if (!groups.Contains(groupName))
                    {
                        list.Add(new SearchTreeGroupEntry(new GUIContent(entryTitle[i]), i + 1));
                        groups.Add(groupName);
                    }

                    groupName += ".";
                }

                var last = entryTitle.Last();
                var entry = new SearchTreeEntry(new GUIContent(last));
                entry.level = entryTitle.Length;
                entry.userData = item;
                list.Add(entry);
            }

            return list;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            EntrySelected?.Invoke(SearchTreeEntry.userData);
            return true;
        }
    }
}