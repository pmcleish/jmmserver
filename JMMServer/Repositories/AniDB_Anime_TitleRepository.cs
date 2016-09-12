﻿using System;
using System.Collections.Generic;
using System.Linq;
using JMMServer.Collections;
using System.Linq;
using FluentNHibernate.Utils;
using JMMServer.Entities;
using JMMServer.Repositories.NHibernate;
using NHibernate;
using NHibernate.Criterion;
using NutzCode.InMemoryIndex;

namespace JMMServer.Repositories
{
    public class AniDB_Anime_TitleRepository
    {
        private static PocoCache<int, AniDB_Anime_Title> Cache;
        private static PocoIndex<int, AniDB_Anime_Title, int> Animes;
        public static void InitCache()
        {
            string t = "AniDB_Anime_Title";
            ServerState.Instance.CurrentSetupStatus = string.Format(JMMServer.Properties.Resources.Database_Cache, t, string.Empty);
            AniDB_Anime_TitleRepository repo = new AniDB_Anime_TitleRepository();
            Cache = new PocoCache<int, AniDB_Anime_Title>(repo.InternalGetAll(), a => a.AniDB_Anime_TitleID);
            Animes = new PocoIndex<int, AniDB_Anime_Title, int>(Cache, a => a.AnimeID);
        }
        internal List<AniDB_Anime_Title> InternalGetAll()
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var objs = session
                    .CreateCriteria(typeof(AniDB_Anime_Title))
                    .List<AniDB_Anime_Title>();

                return new List<AniDB_Anime_Title>(objs);
                ;
            }
        }
        public void Save(AniDB_Anime_Title obj)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                // populate the database
                using (var transaction = session.BeginTransaction())
                {
                    session.SaveOrUpdate(obj);
                    transaction.Commit();
                    Cache.Update(obj);
                }
            }
        }
        public void Save(IEnumerable<AniDB_Anime_Title> objs)
        {
            if (!objs.Any())
                return;
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                // populate the database
                using (var transaction = session.BeginTransaction())
                {
                    foreach (AniDB_Anime_Title obj in objs)
                    {
                        session.SaveOrUpdate(obj);
                    }
                    transaction.Commit();
                    foreach (AniDB_Anime_Title obj in objs)
                    {
                        Cache.Update(obj);
                    }
                }
            }
        }
        public AniDB_Anime_Title GetByID(int id)
        {
            return Cache.Get(id);
            /*
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                return session.Get<AniDB_Anime_Title>(id);
            }*/
        }

        public List<AniDB_Anime_Title> GetAll()
        {
            return Cache.Values.ToList();
            /*
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var objs = session
                    .CreateCriteria(typeof(AniDB_Anime_Title))
                    .List<AniDB_Anime_Title>();

                return new List<AniDB_Anime_Title>(objs);
                ;
            }*/
        }

        public List<AniDB_Anime_Title> GetByAnimeID(int id)
        {
            return Animes.GetMultiple(id);
            /*
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var titles = session
                    .CreateCriteria(typeof(AniDB_Anime_Title))
                    .Add(Restrictions.Eq("AnimeID", id))
                    .List<AniDB_Anime_Title>();

                return new List<AniDB_Anime_Title>(titles);
            }*/
        }

        public List<AniDB_Anime_Title> GetByAnimeID(ISessionWrapper session, int id)
        {
            return Animes.GetMultiple(id);
            /*
            var titles = session
                .CreateCriteria(typeof(AniDB_Anime_Title))
                .Add(Restrictions.Eq("AnimeID", id))
                .List<AniDB_Anime_Title>();

            return new List<AniDB_Anime_Title>(titles);*/
        }

        public ILookup<int, AniDB_Anime_Title> GetByAnimeIDs(ISessionWrapper session, ICollection<int> ids)
        {
            if (session == null)
                throw new ArgumentNullException("session");
            if (ids == null)
                throw new ArgumentNullException("ids");

            if (ids.Count == 0)
            {
                return EmptyLookup<int, AniDB_Anime_Title>.Instance;
            }

            var titles = session.CreateCriteria<AniDB_Anime_Title>()
                .Add(Restrictions.InG(nameof(AniDB_Anime_Title.AnimeID), ids))
                .List<AniDB_Anime_Title>()
                .ToLookup(t => t.AnimeID);

            return titles;
        }

        public List<AniDB_Anime_Title> GetByAnimeIDLanguageTypeValue(int animeID, string language, string titleType,
            string titleValue)
        {
            return
                Animes.GetMultiple(animeID)
                    .Where(
                        a =>
                            a.Language.Equals(language, StringComparison.InvariantCultureIgnoreCase) &&
                            a.Title.Equals(titleValue, StringComparison.InvariantCultureIgnoreCase) &&
                            a.TitleType.Equals(titleType, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
            /*
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var titles = session
                    .CreateCriteria(typeof(AniDB_Anime_Title))
                    .Add(Restrictions.Eq("AnimeID", animeID))
                    .Add(Restrictions.Eq("TitleType", titleType))
                    .Add(Restrictions.Eq("Language", language))
                    .Add(Restrictions.Eq("Title", titleValue))
                    .List<AniDB_Anime_Title>();

                return new List<AniDB_Anime_Title>(titles);
            }*/
        }

        /// <summary>
        /// Gets all the anime titles, but only if we have the anime locally
        /// </summary>
        /// <returns></returns>
        public List<AniDB_Anime_Title> GetAllForLocalSeries()
        {
            return
                new AnimeSeriesRepository().GetAll()
                    .SelectMany(a => GetByAnimeID(a.AniDB_ID))
                    .Where(a => a != null)
                    .Distinct()
                    .ToList();
            /*
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var titles =
                    session.CreateQuery(
                        "FROM AniDB_Anime_Title aat WHERE aat.AnimeID IN (Select aser.AniDB_ID From AnimeSeries aser)")
                        .List<AniDB_Anime_Title>();

                return new List<AniDB_Anime_Title>(titles);
            }*/
        }

        public void Delete(int id)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                // populate the database
                using (var transaction = session.BeginTransaction())
                {
                    AniDB_Anime_Title cr = GetByID(id);
                    if (cr != null)
                    {
                        Cache.Remove(cr);
                        session.Delete(cr);
                        transaction.Commit();
                    }
                }
            }
        }
        public void Delete(IEnumerable<AniDB_Anime_Title> tits)
        {
            if (!tits.Any())
                return;
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                // populate the database
                using (var transaction = session.BeginTransaction())
                {
                    foreach (AniDB_Anime_Title cr in tits)
                    {
                        Cache.Remove(cr);
                        session.Delete(cr);
                    }
                    transaction.Commit();
                }
            }
        }
    }
}