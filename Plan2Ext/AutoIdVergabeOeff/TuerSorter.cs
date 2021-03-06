﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace Plan2Ext.AutoIdVergabeOeff
{
    internal class TuerSorter : Sorter
    {
        private readonly IConfigurationHandler _configurationHandler;
        private readonly IPalette _palette;
        private readonly IComparer<string> _raumNummernComparer;
        private int _currentNr;

        public TuerSorter(ConfigurationHandler configurationHandler, IPalette palette, IComparer<string> raumNummernComparer)
        {
            _configurationHandler = configurationHandler;
            _palette = palette;
            _raumNummernComparer = raumNummernComparer;
        }

        public void Sort(IEnumerable<ITuerInfo> tuerInfos)
        {
            _currentNr = _palette.TuerNr;
            var arr = tuerInfos.ToArray();
            SortTuerenInRaum(arr);
            try
            {
                UcsToAnsicht();
                SortFromLeftToRight(arr);
            }
            finally
            {
                UcsRestore();
            }

            _palette.TuerNr = _currentNr;
        }

        private void SortFromLeftToRight(ITuerInfo[] tuerInfos)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var tuerInfosWithoutRaumblock = tuerInfos.Where(x => x.RaumblockId.IsNull).ToArray();
            NrFromLeftToRight(doc, tuerInfosWithoutRaumblock);
        }

        private void NrFromLeftToRight(Document doc, ITuerInfo[] tuerInfos)
        {
            using (var transaction = doc.TransactionManager.StartTransaction())
            {
                var ordered = tuerInfos.OrderBy(x => Globs.TransWcsUcs(x.InsertPoint).X).ToArray();
                Renumber(ordered, transaction);

                transaction.Commit();
            }
        }


        private class TuerInfoAndCompareValue
        {
            public ITuerInfo TuerInfo { get; set; }
            public string CompareValue { get; set; }
        }

        private void SortTuerenInRaum(ITuerInfo[] tuerInfos)
        {
            var raumTuerInfos = tuerInfos.Where(x => !x.RaumblockId.IsNull).ToArray();
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (var transaction = doc.TransactionManager.StartTransaction())
            {
                if (transaction == null) throw new InvalidOperationException("Transcation is null!");
                var tinfoAndCompareValues = raumTuerInfos.Select(x => new TuerInfoAndCompareValue(){TuerInfo = x, CompareValue = GetCompareValueFromRaumblock(x.RaumblockId, transaction)}).ToArray();
                var ordered = tinfoAndCompareValues.OrderBy(x => x.CompareValue, _raumNummernComparer).Select(x => x.TuerInfo).ToArray();
                Renumber(ordered, transaction);
                transaction.Commit();
            }
        }

        private string GetCompareValueFromRaumblock(ObjectId argRaumblockId, Transaction transaction)
        {
            var raumblockRef = (BlockReference)transaction.GetObject(argRaumblockId, OpenMode.ForRead);
            var nrAtt = Globs.GetAttributEntities(raumblockRef, transaction).FirstOrDefault(x =>
                string.Compare(x.Tag, _configurationHandler.RaumIdAttName, StringComparison.OrdinalIgnoreCase) == 0);
            if (nrAtt == null)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Raumblock mit Handle {0} hat kein Attribut {1}!", raumblockRef.Handle.ToString(),
                    _configurationHandler.RaumIdAttName));
            return nrAtt.TextString;
        }

        private void Renumber(ITuerInfo[] ordered, Transaction transaction)
        {
            foreach (var tuerInfo in ordered)
            {
                var blockReference = (BlockReference)transaction.GetObject(tuerInfo.Oid, OpenMode.ForRead);
                var nrAtt = Globs.GetAttributEntities(blockReference, transaction).FirstOrDefault(x =>
                    string.Compare(x.Tag, _configurationHandler.TuerNrAttName, StringComparison.OrdinalIgnoreCase) == 0);
                if (nrAtt == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        "Türblock mit Handle {0} hat kein Attribut {1}!", blockReference.Handle.ToString(),
                        _configurationHandler.TuerNrAttName));
                nrAtt.UpgradeOpen();
                nrAtt.TextString = _palette.TuerPrefix + _currentNr.ToString().PadLeft(3, '0');
                nrAtt.DowngradeOpen();
                _currentNr++;
            }
        }

    }
}
