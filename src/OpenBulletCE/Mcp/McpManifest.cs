namespace OpenBulletCE.Mcp;

/// <summary>
/// Declares the MCP feature version and the full capability manifest so agents
/// can discover what this server supports via get_mcp_version.
/// </summary>
internal static class McpManifest
{
    /// <summary>Bump when tools or their schemas change.</summary>
    public const string Version = "1.0.0";

    public record FeatureGroup(string Group, string[] Tools);

    public static readonly FeatureGroup[] Features =
    {
        new("server", new[] { "get_server_info", "get_mcp_version", "get_mcp_guide" }),
        new("settings", new[] { "get_settings", "update_settings", "get_rurilib_settings", "update_rurilib_settings", "get_environment_info" }),
        new("wordlists", new[] { "list_wordlists", "get_wordlist", "create_wordlist", "delete_wordlist", "list_cookie_lists", "get_cookie_list" }),
        new("hits", new[] { "list_hits", "get_hit", "search_hits", "delete_hits", "export_hits" }),
        new("configs", new[] { "list_configs", "get_config", "create_config", "update_config_metadata", "get_config_settings", "update_config_settings", "set_config_settings", "get_config_readme", "update_config_readme", "delete_config", "reload_configs", "get_fresh_config" }),
        new("script", new[] { "get_loliscript", "update_loliscript", "get_script_map", "get_script_region", "find_in_script", "patch_script_region", "validate_script", "write_config_script", "read_script_file", "validate_loliscript", "get_block_template" }),
        new("stack", new[] { "get_config_stack", "get_stack_overview", "get_block", "add_block", "remove_block", "move_block", "clone_block", "update_block", "replace_block", "undo_last_change", "validate_stack", "commit_stack" }),
        new("debug", new[] { "debug_config", "quick_test_config" }),
        new("blocks", new[] { "list_available_blocks", "get_block_details" }),
        new("jobs", new[] { "list_jobs", "get_job", "create_job", "start_job", "stop_job", "abort_job", "pause_job", "resume_job", "set_bots_amount", "delete_job", "get_job_log", "get_job_hits" }),
        new("plugins", new[] { "list_plugins", "list_installed_plugins" }),
        new("har", new[] { "analyze_har_file", "generate_config_from_har" }),
        new("tls", new[] { "get_cloudflare_guidance", "analyze_cloudflare_protection", "enable_curl_impersonation" }),
        new("workstate", new[] { "save_checkpoint", "get_checkpoint", "clear_checkpoint" }),
    };

    public record ChangelogEntry(string Version, string Date, string[] Changes);

    public static readonly ChangelogEntry[] Changelog =
    {
        new("1.0.0", "2026-02-12", new[]
        {
            "Initial MCP server for OpenBullet Cookie Edition (LoliScript engine)",
            "Full config CRUD + surgical script/stack editing for .lce configs",
            "In-app debugger via debug_config / quick_test_config",
            "Job lifecycle on the CE runner engine (start/stop/abort, bots, hits)",
            "HAR analysis + LoliScript config generation",
            "Block reference for all LoliScript block types incl. plugins"
        })
    };
}
