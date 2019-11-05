Documentation
===============
Bienvenue sur la documentation
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

La documentation du site se veut être interactive, participative et couvrir les différents projets et applicatifs qu'offre Hybrid Technologies Solutions. Vous y trouverez , en naviguant à l'aide du menu sur la gauche, plus d'informations sur l'utilisation de notre framework CQELight pour la conception d'application centralisées sur le métier, tout comme des conseils généraux pour la modélisation du domaine.

N'hésitez pas à parcourir la documentation ! En cas de manque, oubli ou erreur, vous pouvez créer une issue sur GitHub, nous la traiterons et procéderons à la mise à jour de la documentation.

.. toctree::
   :maxdepth: 3
   :hidden:
   :caption: Introduction
   
   intro/cqelight
   intro/getting-started
   
.. toctree::
   :maxdepth: 3
   :hidden:
   :caption: CQELight
   
   cqelight/domain-modeling
   cqelight/command-queries
   cqelight/ioc
   cqelight/data-modeling
   cqelight/data-access-layer
   cqelight/event-sourcing
   cqelight/dispatcher-conf
   cqelight/bootstrapper
   cqelight/extensibility
   cqelight/unit-tests
   
.. toctree::
   :maxdepth: 3
   :hidden:
   :caption: Exemple complet
   
   cqelight-sample/scenario
   cqelight-sample/domain-modeling
   cqelight-sample/commands
   cqelight-sample/events
   cqelight-sample/data-access
   cqelight-sample/ioc
   cqelight-sample/queries
   cqelight-sample/event-sourcing
   cqelight-sample/unit-tests
   cqelight-sample/conclusion

.. toctree::
   :maxdepth: 3
   :hidden:
   :caption: Extensions
   
   extensions/ioc-autofac
   extensions/dal-efcore
   extensions/bus-inmemory
   extensions/bus-rabbitmq
   extensions/event-efcore

.. toctree::
   :maxdepth: 3
   :hidden:
   :caption: Migration

   migrations/v1_1_1
   