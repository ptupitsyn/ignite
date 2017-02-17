package org.apache.ignite.platform.plugin.cache;

import org.apache.ignite.binary.BinaryRawReader;
import org.apache.ignite.configuration.CacheConfiguration;
import org.apache.ignite.plugin.CachePluginConfiguration;
import org.apache.ignite.plugin.platform.PlatformCachePluginConfigurationClosure;

import java.util.ArrayList;
import java.util.Collections;

/**
 * Test closure factory.
 */
public class PlatformTestCachePluginConfigurationClosureFactory implements PlatformCachePluginConfigurationClosure {
    /** {@inheritDoc} */
    @Override public void apply(CacheConfiguration cacheConfiguration, BinaryRawReader reader) {
        ArrayList<CachePluginConfiguration> cfgs = new ArrayList<>();

        if (cacheConfiguration.getPluginConfigurations() != null) {
            Collections.addAll(cfgs, cacheConfiguration.getPluginConfigurations());
        }

        PlatformTestCachePluginConfiguration plugCfg = new PlatformTestCachePluginConfiguration();

        plugCfg.setPluginProperty(reader.readString());

        cfgs.add(plugCfg);

        cacheConfiguration.setPluginConfigurations(cfgs.toArray(new CachePluginConfiguration[cfgs.size()]));
    }
}
