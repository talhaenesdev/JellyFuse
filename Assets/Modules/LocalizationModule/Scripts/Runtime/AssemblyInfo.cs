using System.Runtime.CompilerServices;

// The module's Commands, Model, constants and CSV reader stay internal; its own test module is the
// one reader allowed past that.
[assembly: InternalsVisibleTo("Modules.Localization.Test")]
