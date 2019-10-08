Bus RabbitMQ
============

`RabbitMQ <https://www.rabbitmq.com/>`_ est système de messaging basé sur le protocole AMQP bien connu, extrêmement stable et rapide. Afin de profiter de cet outil pour le messaging entre vos services, il est simplement nécessaire d'ajouter le package ``CQELight.Bus.RabbitMQ`` à votre projet. Comme tous les autres, ce package est disponible sur NuGet.

Cette extension fonctionne comme les autres et s'utilise en appelant la méthode d'extension dédiée sur le bootstrapper. 

.. warning:: Les informations qui suivent ne sont valides que pour la version 1.1. Si vous n'avez pas migré depuis la version 1.0x, il est vivement recommandé de le faire afin de profiter des dernières améliorations. 

Configuration
^^^^^^^^^^^^^
Depuis la v1.1, la méthode d'extension a été revue, et est maintenant beaucoup plus extensible : ``UseRabbitMQ``. Cette méthode prends en paramètre les informations suivantes : 
- L'objet ``RabbitConnectionInfos`` contient les informations nécessaire afin de pouvoir se connecter à l'instance (ou au cluster) RabbitMQ. Cet objet est construit à partir d'une ``ConnectionFactory`` (objet issu du driver RabbitMQ) et demande également un nom de service. Ce nom est utilisé pour le traitement des messages.
- L'objet ``RabbitNetworkInfos`` contient les informations de la topologie du réseau entre exchanges et queues que vous avez mis en place. Certaines informations peuvent être automatisées. Cet objet est construit à partir du nom du service ainsi que d'un flag de préconfiguration qui peut prendre plusieurs valeurs : ``Custom``, ``SingleExchange`` et ``ExchangePerService``. Ces valeurs seront détaillées plus loin dans cette documentation.
- Deux lambdas éventuelles de configuration supplémentaire, qui seront appellées resepectivement sur la configuration d'émission et sur la configuration de réception.

Configuration de la connexion
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Les informations de connexion à RabbitMQ sont hébergées dans la classe ``RabbitConnectionInfos``. Cette classe doit être instanciée depuis un ``ConnectionFactory``. L'avantage de cette approche et que le développeur a la totale liberté de la façon dont il veut configurer sa connexion, lui laissant libre l'approche adaptée à son infrastructure.

Une version par défaut serait par exemple 
::

    RabbitConnectionInfos.FromConnectionFactory(
        new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        },
        "service1"
    );

Configuration du réseau
^^^^^^^^^^^^^^^^^^^^^^^
Le protocole AMQP repose sur deux concepts principaux : les exchanges et les queues. Chaque service doit utiliser les exchanges pour communiquer, et les queues pour écouter. Une topologie simple veut que chaque service possède sa propre queue, ainsi qu'un exchange central permettant l'écoute selon les critères définis.

Afin de créer une configuration réseau configurable, il faut utiliser la méthode statique ``RabbitNetworkInfos.GetConfigurationFor("service1", RabbitMQExchangeStrategy.SingleExchange)``. Dans cet exemple, on crée une configuration de base SingleExchange (un seul exchange pour tout le système) pour le service appelé "service1".

Les différents modes sont les suivants :
- Custom : renvoie une configuration réseau prête à la configuration, où tout est à définir. Il n'existe aucun exchange, aucune queue et aucun binding.
- SingleExchange : la configuration contient un exchange préconfiguré qui se nomme "cqelight_global_exchange". Cet exchange est défini de base en mode "topic", c'est à dire qu'il est nécessaire d'avoir des clé de routage (routing key) pour permettre l'arrivée au bon destinataire. Une queue a également été créée, et porte un nom par convention : "[NOM_DU_SERVICE]_queue". Cette queue est automatiquement bindée à l'exchange central avec comme clé de routage le nom du service.
- ExchangePerService : la configuration contient deux éléments préconfigurés, un exchange portant le nom par convention : "[NOM_DU_SERVICE]_exchange" et une queue avec la même convention que pour SingleExchange. Aucun binding n'est défini, ils sont à créer selon votre configuration.

.. note:: Chaque objet créé par une configuration de base reste modifiable. Cette configuration du réseau sera utilisée par le publisher et/ou par le subscriber afin de paramétrer l'instance RabbitMQ.

.. note:: RabbitMQ ayant un fonctionnement déclaratif, il n'y a pas d'incidences à déclarer plusieurs fois le même élément. Il est même préférable que ça soit déclaré plusieurs fois plutôt que pas du tout, sous peine d'avoir une erreur dans ce dernier cas.

Lambdas de configuration
^^^^^^^^^^^^^^^^^^^^^^^^
Les deux lambdas de configuration permettent de renseigner des éléments supplémentaires. 

Côté subscriber :
- Il est possible de définir le flag ``UseDeadLetterQueue`` (true par défaut). Ce flag permet une meilleure maintenance de votre système, mettant dans une queue dédiée ("cqelight_dead_letter_queue") les messages n'arrivant pas être traités.
- Le flag ``DispatchInMemory`` (true par défaut) permet de définir si les messages obtenus depuis une queue RabbitMQ doivent être envoyés dans le bus in-memory.
- ``EventCustomCallback`` et ``CommandCustomCallback`` permet de définir des callbacks personnalisés qui seront invoqués en réception.
- ``AckStrategy`` est une énumération qui permet de déterminer la stratégie à utiliser lors de la réception d'un message. Par défaut, la valeur ``AckOnSucces`` est utilisée, c'est à dire qu'on "ack" (comprendre suppression) le message de la queue uniquement lorsque le messages est correctement traité dans le pipeline en mémoire (callback et bus in-memory inclus). L'autre valeur est ``AckOnReceive``, auquel cas le message est "ack" à réception.

Côté publisher :
- Il est possible de définir finement la durée de vie de certaines événements dans une queue : ``EventsLifetime``. Cela permet de leur donner une durée d'expiration à laquelle, s'ils y arrivent, ils seront placés dans la queue des messages non traités. La valeur par défaut est d'une heure pour tous les événements.
- Il est possible de définir un autre serializer pour les messages destinés à être émis : ``Serializer``. Ce serializer permet de définir la forme sous laquelle la donnée est compressée dans l'enveloppe. Par défaut, le fonctionnement est en JSON.
- Il est possible de définir une routing key factory : ``RoutingKeyFactory``. Cet objet calcule selon vos règles la clé de routage utilisée par RabbitMQ. Le comportement par défaut est pas de routing key pour les événements (broadcast) et la première partie du namespace pour les commands.

Utilisation
^^^^^^^^^^^
Une fois que la configuration est effectuée dans le bootstrapper, aucune action manuelle supplémentaire n'est nécessaire, le système se charge de démarrer le serveur d'écoute. Pour envoyer les messages, vous pouvez continuer à utiliser le ``CoreDispatcher`` ou une implémentation du ``IDispatcher`` qui saura router les informations vers le bus RabbitMQ, comme auparavant.

Migration depuis la version 1.0x
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Si vous utilisiez déjà le provider RabbitMQ en version 1.0x, rien n'a changé, les APIs précédentes sont toujours présentes, mais elles ont été dépréciées. Elles seront supprimées en 2.0, c'est pourquoi il est recommandé de migrer votre code.

Défintion du réseau
^^^^^^^^^^^^^^^^^^^
En 1.0x, le réseau fonctionnait de façon exclusive en mode SingleExchange sans routing key. Il est recommandé de définir le réseau suivant (en remplaçant ``serviceName`` par la valeur que vous aviez fixé dans votre configuration comme ``emiter``)  :

::

    var network = RabbitNetworkInfos.GetConfigurationFor(
	   "serviceName", 
	   RabbitMQExchangeStrategy.SingleExchange);
    network.ServiceQueueDescriptions.Add(
	   new RabbitQueueDescription("cqelight.events.serviceName"));
	
Il vous reste à changer votre bootstrapper et remplacer les méthodes ``UseRabbitMQClientBus`` et ``UseRabbitMQServer`` par ``UseRabbitMQ`` :

::

    new Bootstrapper()
    bootstrapper.UseRabbitMQ(
	   connectionInfos: null, //Your connection infos
	   networkInfos: network, //Previously defined
	   subscriberConfiguration: (c) => {
	      c.UseDeadLetterQueue = true; // If used
	      c.DispatchInMemory = true; // If used
	      c.EventCustomCallback = (e) => {}; // If used
	   })
	.Bootstrapp();

Vous restez libre de modifier la topologie réseau et de profiter des nouvelles options suite à ces changements.