using System;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.BroadcastheNet;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tags;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class ReleaseOriginSpecifciation : IDecisionEngineSpecification
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly Logger _logger;

        private readonly ITagService _tagService;

        public ReleaseOriginSpecifciation(IIndexerFactory indexerFactory, Logger logger, ITagService tagService)
        {
            _indexerFactory = indexerFactory;
            _logger = logger;
            _tagService = tagService;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;


        public Decision IsSatisfiedBy(RemoteEpisode remoteEpisode, SearchCriteriaBase searchCriteria)
        {
            foreach (var tag in remoteEpisode.Series.Tags)
            {
                if (_tagService.GetTag(tag).Label.ToLower() == "skip-origin")
                {
                    return Decision.Accept();
                }
            }
            
            var torrentInfo = remoteEpisode.Release as TorrentInfo;

            if (torrentInfo == null || torrentInfo.IndexerId == 0)
            {
                return Decision.Accept();
            }

            IndexerDefinition indexer;
            try
            {
                indexer = _indexerFactory.Get(torrentInfo.IndexerId);
            }
            catch (ModelNotFoundException)
            {
                _logger.Debug("Indexer with id {0} does not exist, skipping origin check", torrentInfo.IndexerId);
                return Decision.Accept();
            }

            if (indexer.Settings is BroadcastheNetSettings torrentIndexerSettings)
            {
                var origin = torrentInfo.Origin.ToLower();
                
                if (torrentIndexerSettings.RejectSceneReleases && origin == "scene")
                {
                    return Decision.Reject("Origin \"Scene\" blacklisted by indexer settings");
                }

                if (torrentIndexerSettings.RejectP2PReleases && origin == "p2p")
                {
                    return Decision.Reject("Origin \"P2P\" blacklisted by indexer settings");
                }

                if (torrentIndexerSettings.RejectUserReleases && origin == "user")
                {
                    return Decision.Reject("Origin \"User\" blacklisted by indexer settings");
                }

                if (torrentIndexerSettings.RejectInternalReleases && origin == "internal")
                {
                    return Decision.Reject("Origin \"Internal\" blacklisted by indexer settings");
                }
            }

            return Decision.Accept();
        }
    }
}
