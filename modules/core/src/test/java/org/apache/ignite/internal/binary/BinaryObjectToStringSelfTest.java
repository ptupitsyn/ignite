package org.apache.ignite.internal.binary;

import java.util.Arrays;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import org.apache.ignite.binary.BinaryObject;
import org.apache.ignite.configuration.IgniteConfiguration;
import org.apache.ignite.spi.discovery.tcp.TcpDiscoverySpi;
import org.apache.ignite.spi.discovery.tcp.ipfinder.TcpDiscoveryIpFinder;
import org.apache.ignite.spi.discovery.tcp.ipfinder.vm.TcpDiscoveryVmIpFinder;
import org.apache.ignite.testframework.junits.common.GridCommonAbstractTest;

/**
 * Tests for {@code BinaryObject.toString()}.
 */
public class BinaryObjectToStringSelfTest extends GridCommonAbstractTest {
    /** */
    private static final TcpDiscoveryIpFinder IP_FINDER = new TcpDiscoveryVmIpFinder(true);

    /** {@inheritDoc} */
    @Override protected IgniteConfiguration getConfiguration(String gridName) throws Exception {
        IgniteConfiguration cfg = super.getConfiguration(gridName);

        cfg.setMarshaller(new BinaryMarshaller());
        cfg.setDiscoverySpi(new TcpDiscoverySpi().setIpFinder(IP_FINDER));

        return cfg;
    }

    /** {@inheritDoc} */
    @Override protected void beforeTest() throws Exception {
        startGrid();
    }

    /** {@inheritDoc} */
    @Override protected void afterTest() throws Exception {
        stopAllGrids();
    }

    /**
     * @throws Exception If failed.
     */
    @SuppressWarnings("unchecked")
    public void testToString() throws Exception {
        MyObject obj = new MyObject();

        obj.arr = new Object[] {111, "aaa", obj};
        obj.col = Arrays.asList(222, "bbb", obj);

        obj.map = new HashMap();

        obj.map.put(10, 333);
        obj.map.put(20, "ccc");
        obj.map.put(30, obj);

        BinaryObject bo = grid().binary().toBinary(obj);

        // Check that toString() doesn't fail with StackOverflowError or other exceptions.
        bo.toString();
    }

    /**
     */
    private static class MyObject {
        /** Object array. */
        private Object[] arr;

        /** Collection. */
        private Collection col;

        /** Map. */
        private Map map;
    }
}
