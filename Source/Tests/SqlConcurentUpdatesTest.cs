﻿using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Terminals.Data;
using Favorite = Terminals.Data.DB.Favorite;

namespace Tests
{
    [TestClass]
    public class SqlConcurentUpdatesTest
    {
        private SqlTestsLab lab;
        private IFavorite updatedFavorite;
        private bool updateEventCatched;

        private bool removedEventCatched;

        private const string TEST_NAME = "second";

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        private IFavorites PrimaryFavorites
        {
            get
            {
                return this.lab.Persistence.Favorites;
            }
        }

        private IFavorites SecondaryFavorites
        {
            get
            {
                return this.lab.SecondaryPersistence.Favorites;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.lab = new SqlTestsLab();
            this.lab.InitializeTestLab();
        }

        [TestCleanup]
        public void TestClose()
        {
            // do not clean up the store here, because of waiting for background threads
            // this.lab.ClearTestLab();
        }

        [TestMethod]
        public void TestPeriodicalUpdates()
        {
            this.lab.AddFavoriteToPrimaryPersistence();

            // assign event handler before another changes to catch all of them
            this.lab.Persistence.Dispatcher.FavoritesChanged +=
                new FavoritesChangedEventHandler(this.OnPrimaryStoreFavoritesChanged);

            var favoriteB = this.SecondaryFavorites.FirstOrDefault() as Favorite;
            favoriteB.Name = TEST_NAME;
            this.SecondaryFavorites.Update(favoriteB);

            ISynchronizeInvoke control = new Control();
            this.lab.Persistence.AssignSynchronizationObject(control);
            // refresh interval is set to 2 sec. by default
            Thread.Sleep(10000);

            Assert.IsTrue(this.updateEventCatched, "Data changed event wasnt received");
            Assert.AreEqual(this.updatedFavorite.Name, TEST_NAME, "The updated favorite wasnt refreshed");
        }

        private void OnPrimaryStoreFavoritesChanged(FavoritesChangedEventArgs args)
        {
            this.updatedFavorite = args.Updated.FirstOrDefault();
            this.updateEventCatched = true;
        }

        [TestMethod]
        public void TestSaveOnAlreadyUpdatedFavorite()
        {
            Favorite favoriteA = this.lab.AddFavoriteToPrimaryPersistence();
            var favoriteB = this.SecondaryFavorites.FirstOrDefault() as Favorite;
            this.lab.Persistence.Dispatcher.FavoritesChanged += 
                new FavoritesChangedEventHandler(this.OnUpdateAlreadyUpdatedFavoritesChanged);

            this.UpdateFavorite(favoriteA, this.PrimaryFavorites);
            this.UpdateFavorite(favoriteB, this.SecondaryFavorites);
            // last update wins
            this.UpdateFavorite(favoriteA, this.PrimaryFavorites);
            this.SecondaryFavorites.Delete(favoriteB);
            // next update shouldnt fail on deleted favorite
            this.UpdateFavorite(favoriteA, this.PrimaryFavorites);
            Assert.IsTrue(this.removedEventCatched, "Persistence didnt catched the already removed favorite update event.");
        }

        private void OnUpdateAlreadyUpdatedFavoritesChanged(FavoritesChangedEventArgs args)
        {
            if (args.Removed.Count > 0)
                this.removedEventCatched = true;
        }

        private void UpdateFavorite(Favorite toUpdate, IFavorites targetPersistence)
        {
            toUpdate.Name += " Changed";
            targetPersistence.Update(toUpdate);
        }
    }
}
