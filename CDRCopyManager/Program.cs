using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

class Program
{
    private static IConfiguration Configuration;

    static async Task Main(string[] args)
    {
        // Charger la configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        Configuration = builder.Build();

        // Lire les paramètres de configuration
        string dossierA = Configuration["DossierSource"];
        string dossierB = Configuration["DossierDestination"];
        string[] prefixesFichiers = Configuration["PrefixesFichiers"].Split(',');
        string[] suffixesFichiers = Configuration["SuffixesFichiers"].Split(',');
        string dossierLogs = Configuration["DossierLogs"];

        // Vérifier que les dossiers existent
        if (!Directory.Exists(dossierA))
        {
            Console.WriteLine($"Le dossier source {dossierA} n'existe pas.");
            return;
        }

        if (!Directory.Exists(dossierB))
        {
            Console.WriteLine($"Le dossier de destination {dossierB} n'existe pas.");
            return;
        }

        // Créer le dossier des logs s'il n'existe pas
        Directory.CreateDirectory(dossierLogs);

        // Créer un fichier journal avec un timestamp
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFilePath = Path.Combine(dossierLogs, $"log_{timestamp}.txt");
        using StreamWriter logWriter = new StreamWriter(logFilePath, append: true);

        // Obtenir la liste des fichiers du dossier A
        var fichiers = Directory.GetFiles(dossierA);

        // Filtrer les fichiers en fonction des préfixes et suffixes spécifiés
        var fichiersFiltres = fichiers.Where(fichier =>
        {
            var fichierInfo = new FileInfo(fichier);
            string nomFichier = fichierInfo.Name;
            return prefixesFichiers.Any(prefix => nomFichier.StartsWith(prefix)) &&
                   suffixesFichiers.Any(suffix => nomFichier.EndsWith(suffix));
        });

        // Traiter les fichiers en parallèle
        var tasks = fichiersFiltres.Select(async fichier =>
        {
            var fichierInfo = new FileInfo(fichier);

            // Vérifier si la taille du fichier est supérieure à 1 Ko
            if (fichierInfo.Length > 1024)
            {
                // Définir le chemin de destination
                string cheminDestination = Path.Combine(dossierB, fichierInfo.Name);

                try
                {
                    // Copier le fichier vers le dossier B
                    File.Copy(fichier, cheminDestination, true); // 'true' écrase le fichier s'il existe déjà
                    logWriter.WriteLine($"[{DateTime.Now}] Fichier {fichierInfo.Name} copié avec succès.");
                }
                catch (Exception ex)
                {
                    logWriter.WriteLine($"[{DateTime.Now}] Erreur lors de la copie du fichier {fichierInfo.Name}: {ex.Message}");
                }
            }
        });

        // Attendre la fin de toutes les tâches
        await Task.WhenAll(tasks);

        logWriter.WriteLine($"[{DateTime.Now}] Opération terminée.");
        Console.WriteLine("Opération terminée. Consultez le fichier journal pour les détails.");
    }
}
