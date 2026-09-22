using System;
using System.Collections.Generic;

using Abc.Unity;

namespace Abc.Unity.Editor
{
    internal sealed class ActorWorldExplorerSearch
    {
        private enum Scope
        {
            Any,
            Name,
            Tag,
            Data,
            Behaviour
        }

        private readonly struct Term
        {
            public Term(Scope scope, string value)
            {
                SearchScope = scope;
                Value = value;
            }

            public Scope SearchScope { get; }
            public string Value { get; }
        }

        private readonly List<Term> _terms = new List<Term>();

        public bool RequiresModules { get; private set; }

        public void SetQuery(string query)
        {
            _terms.Clear();
            RequiresModules = false;

            if (string.IsNullOrWhiteSpace(query))
                return;

            var parts = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var separator = part.IndexOf(':');
                var scope = separator > 0 ? ParseScope(part.Substring(0, separator)) : Scope.Any;
                var value = scope == Scope.Any ? part : part.Substring(separator + 1);
                if (value.Length == 0)
                    continue;

                _terms.Add(new Term(scope, value));
                RequiresModules |= scope == Scope.Any || scope == Scope.Data || scope == Scope.Behaviour;
            }
        }

        public bool Matches(ActorModel actor, IReadOnlyList<IActorModule> modules)
            => Matches(actor.Name, actor.TagValue, modules);

        public bool Matches(string name, ActorTag tag, IReadOnlyList<IActorModule> modules)
        {
            for (var i = 0; i < _terms.Count; i++)
            {
                if (!Matches(_terms[i], name, tag, modules))
                    return false;
            }

            return true;
        }

        private static Scope ParseScope(string prefix)
        {
            switch (prefix.ToLowerInvariant())
            {
                case "name": return Scope.Name;
                case "tag": return Scope.Tag;
                case "data": return Scope.Data;
                case "behaviour":
                case "behavior": return Scope.Behaviour;
                default: return Scope.Any;
            }
        }

        private static bool Matches(Term term, string name, ActorTag tag, IReadOnlyList<IActorModule> modules)
        {
            if (term.SearchScope == Scope.Name)
                return Contains(name, term.Value);

            if (term.SearchScope == Scope.Tag)
                return Contains(tag.ToString(), term.Value) || tag.Value.ToString() == term.Value;

            if (term.SearchScope == Scope.Any &&
                (Contains(name, term.Value) || Contains(tag.ToString(), term.Value)))
                return true;

            if (modules == null)
                return false;

            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module == null || !Contains(module.GetType().Name, term.Value))
                    continue;

                if (term.SearchScope == Scope.Any ||
                    term.SearchScope == Scope.Data && module is IActorData ||
                    term.SearchScope == Scope.Behaviour && module is IActorBehaviour)
                    return true;
            }

            return false;
        }

        private static bool Contains(string value, string search) =>
            value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
