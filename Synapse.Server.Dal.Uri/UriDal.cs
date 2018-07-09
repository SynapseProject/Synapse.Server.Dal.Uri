using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Suplex.Security;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.Server.Dal.Enums;
using Synapse.Server.Dal.Uri;
using Synapse.Server.Dal.Uri.Encryption;
using Synapse.Server.Dal.Uri.Interfaces;
using Synapse.Services;
using Synapse.Services.Controller.Dal;


//namespace Synapse.Services.Controller.Dal { }
public partial class UriDal : IControllerDal
{
    static readonly string CurrentPath = "";

    string _planPath = null;
    string _histPath = null;
    string _splxPath = null;

    SuplexDal _splxDal = null;

    ICloudUriHandler cloudUriHandler;

    //this is a stub feature
    static long PlanInstanceIdCounter = DateTime.Now.Ticks;

    public UriDal()
    {
    }

    internal UriDal(string basePath, bool processPlansOnSingleton = false, bool processActionsOnSingleton = true) : this()
    {
        if( string.IsNullOrWhiteSpace( basePath ) )
            basePath = CurrentPath;

        _planPath = $"{basePath}\\Plans\\";
        _histPath = $"{basePath}\\History\\";
        _splxPath = $"{basePath}\\Security\\";

        EnsurePaths();

        ProcessPlansOnSingleton = processPlansOnSingleton;
        ProcessActionsOnSingleton = processActionsOnSingleton;

        LoadSuplex();
    }


    public object GetDefaultConfig()
    {
        return new UriDalConfig();
    }


    public Dictionary<string, string> Configure(ISynapseDalConfig conifg)
    {
        if( conifg != null && conifg.Config != null )
        {
            string s = YamlHelpers.Serialize( conifg.Config );
            UriDalConfig fsds = YamlHelpers.Deserialize<UriDalConfig>( s );

            _planPath = fsds.PlanFolderPath;
            _histPath = fsds.HistoryFolderPath;
            _splxPath = fsds.Security.FilePath;

            EnsurePaths();

            ProcessPlansOnSingleton = fsds.ProcessPlansOnSingleton;
            ProcessActionsOnSingleton = fsds.ProcessActionsOnSingleton;

            LoadSuplex();

            if(fsds.CloudPlatform ==  CloudPlatform.Azure)
            {
                AzureUriDalConfig azureConfig = YamlHelpers.Deserialize<AzureUriDalConfig>( s );
                cloudUriHandler = new AzureCloudUriHandler( azureConfig );
            }
            else
            {
                AWSUriDalConfig awsConfig = YamlHelpers.Deserialize<AWSUriDalConfig>( s );
                cloudUriHandler = new AWSCloudUriHandler( awsConfig );
            }
            EncryptionHelper.Configure( fsds );

        if( _splxDal == null && fsds.Security.IsRequired )
                throw new Exception( $"Security is required.  Could not load security file: {_splxPath}." );

            if( _splxDal != null )
            {
                _splxDal.LdapRoot = conifg.LdapRoot;
                _splxDal.GlobalExternalGroupsCsv = fsds.Security.GlobalExternalGroupsCsv;
            }
        }
        else
        {
            ConfigureDefaults();
        }

        Dictionary<string, string> props = new Dictionary<string, string>();
        string name = nameof( UriDal );
        props.Add( name, CurrentPath );
        props.Add( $"{name} Plan path", _planPath );
        props.Add( $"{name} History path", _histPath );
        props.Add( $"{name} Security path", _splxPath );
        return props;
    }

    internal void ConfigureDefaults()
    {
        _planPath = $"{CurrentPath}Plans";
        _histPath = $"{CurrentPath}History";
        _splxPath = $"{CurrentPath}Security";

        EnsurePaths();

        ProcessPlansOnSingleton = false;
        ProcessActionsOnSingleton = true;

        LoadSuplex();
    }

    void EnsurePaths()
    {
        //NOT REQUIRED AT THIS POINT OF TIME SINCE THE PATH 
        //FORMED IN AWS AND AZURE AUTOMATICALLY CREATES THE 
        //REQUIRED FOLDER STRUCTURE
    }

    void LoadSuplex()
    {
        string splx =Synapse.Services.Controller.Dal.Utilities.PathCombine( _splxPath, "security.splx" );
        //if( File.Exists( splx ) )
        //    _splxDal = new SuplexDal( splx );
    }


    public bool ProcessPlansOnSingleton { get; set; }
    public bool ProcessActionsOnSingleton { get; set; }
}