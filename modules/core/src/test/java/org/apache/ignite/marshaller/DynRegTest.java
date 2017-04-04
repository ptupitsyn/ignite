package org.apache.ignite.marshaller;

import junit.framework.TestCase;
import org.apache.ignite.Ignite;
import org.apache.ignite.IgniteCache;
import org.apache.ignite.Ignition;
import org.apache.ignite.configuration.IgniteConfiguration;
import org.junit.Test;

public class DynRegTest extends TestCase {
    @Test
    public void testDynReg() {
        IgniteConfiguration cfg = new IgniteConfiguration();
        cfg.setIgniteInstanceName("bzz");

        Ignite ignite1 = Ignition.start(cfg);
        Ignite ignite2 = Ignition.start();

        IgniteCache cache = ignite2.createCache("foos");

        cache.put(1, new Foo());

        Ignition.stop(true);

        ignite2 = Ignition.start();

        cache = ignite2.cache("foos");

        System.out.println("Put to cache...");
        cache.put(2, new Foo());
        System.out.println("Done");

        Ignition.stopAll(true);
    }

    public class Foo {
        public int id;
    }
}
