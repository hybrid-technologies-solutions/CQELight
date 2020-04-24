IoC avec Microsoft.Extensions.DependencyInjection
=================================================

``Microsoft.Extensions.DependencyInjection`` est le fournisseur d'injection de dépendance nativement inclus avec ASP.NET Core. 

Son intégration ne demande aucun effort particulier, car il est déjà dans les arcanes du framework. L'injection de dépendance se configure avec CQELight comme vous avez l'habitude (par le biais du Bootsrapper ou encore à l'aide des interfaces ``IAutoRegisterTypeSingleInstance`` et ``IAutoRegisterType``), mais aussi nativement à l'aide de la méthode ``ConfigureServices`` de votre classe ``Startup``.

.. note:: Si vous avez configuré un site ASP.NET Core comme indiqué sur la page dédiée (accessible ici : :doc:`./asp-core`), vous n'êtes pas obligé de faire appel à la méthode ``UseMicrosoftDependencyInjection`` sur le Bootstrapper. Même si ceci reste possible avec un site ASP.NET Core 2.1, cet appel n'est plus possible en 3.1 (vous n'avez pas accès à la collection de services dans la classe Program). De ce fait, si vous n'avez spécifié aucun service d'IoC au niveau du Bootstrapper, l'utilisation de cette extension comme IoC sera faite automatiquement. Le package requis est ajouté avec l'extension AspCore sans actions supplémentaires.

Pour pouvoir l'utiliser, il vous faut ajouter le package ``CQELight.IoC.Microsoft.Extensions.DependencyInjection`` à votre projet. Le package est disponible sur NuGet. 

Cette extension s'active comme toutes les autres, et s'utilise en appelant la méthode d'extension dédiée sur le bootstrapper. Il suffit d'appeler la méthode ``UseMicrosoftDependencyInjection`` sur votre instance du bootsrapper. Vous devrez passer en paramètre à cette méthode l'instance de votre collection de services (de type ``IServicesCollection``).

Il existe également un paramètre permet d'exclure des assemblies de la recherche automatiquement pour les interfaces ``IAutoRegisterType`` et ``IAutoRegisterTypeSingleInstance``.

::

    new Bootstrapper().UseMicrosoftDependencyInjection(
        new ServiceCollection(),
    // Excluding DLLs from searching to enhance performances (it's a contains searching)
        "CQELight", "Microsoft", "System")   

