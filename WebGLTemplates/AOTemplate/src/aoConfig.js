// import { ArWalletEnum } from './types';

export const AO = {
	token: '0syT13r0s0tgPmIed95bJnuSqaD29HQNN8D3ElLSrsc',
	tokenMirror: 'ptCu-Un-3FF8sZ5zNMYg43zRgSYAGVkjz2Lb0HZmx2M',
	cred: 'Sa0iBLPNyJQrwpTTG-tWLQU-1QeUAJA73DdxGGiKoJc',
	aoClaim: 'U2Bv-LEoFzwAFfBx9MiXNnAfaYRjT4MG9T7sFcVHn20',
	aoMetrics: 'vdpaKV_BQNISuDgtZpLDfDlMJinKHqM3d2NWd3bzeSk',
	delegationOracle: 'cuxSKjGJ-WDB9PzSkVkVVrIBSh3DrYHYz44usQOj5yE',
	stEthPriceOracle: 'wJV8FMkpoeLsTjJ6O7YZEuQgMqj-sDjPHhTeA73RsCc',
	daiPriceOracle: '5q8vpzC5QAKOAJFM26MAKfZw1gwtw7WA_J2861ZiKhI',
	yieldPreferences: 'pGpdfjH4XkjS_GPuFSPlkEJ3buIWWlI8q4-BqG7GiAo',
	yieldHistorian: 'NRP0xtzeV9MHgwLmgD254erUB7mUjMBhBkYkNYkbNEo',
	flpFactory: 'It-_AKlEfARBmJdbJew1nG9_hIaZt0t20wQc28mFGBE',
	piProcess: 'rxxU4g-7tUHGvF28W2l53hxarpbaFR4NaSnOaxx6MIE',
	// delegationOracle: '2AjNEkmSIzUeotKpHFiYEf8sMuh7ph11cjKx66GZdcc', // staging
	// yieldHistorian: 'veRuOU7Y_r_6aEXef8aRtSAzROoOPlujaUdCE6hwJTY', // staging
	// flpFactory: 'JC0_BVWWf7xbmXUeKskDBRQ5fJo8fWgPtaEYMOf-Vbk', // staging
	// piProcess: 'ashzRmPuxsO6xSZulIeZl-rQ-DsFsjwLYc8IIlY-Ots', // staging
};

export const ETH_CONTRACTS = {
	stEth: '0xae7ab96520de3a18e5e111b5eaab095312d7fe84',
	stEthBridge: '0xfE08D40Eee53d64936D3128838867c867602665c',
	dai: '0x6b175474e89094c44da98b954eedeac495271d0f',
	daiBridge: '0x6A1B588B0684dACE1f53C5820111F400B3dbfeBf',
	ethUsdPriceFeed: '0x5f4ec3df9cbd43714fe2740f5e3616155c5b8419',
	daiUsdPriceFeed: '0xAed0c38402a5d19df6E4c03F4E2DceD6e29c1ee9',
};

export const AO_TOKEN_DENOMINATION = Math.pow(10, 12);
export const ETH_TOKEN_DENOMINATION = Math.pow(10, 18);

export const ENDPOINTS = {
	arBalance: (address) => `https://arweave.net/wallet/${address}/balance`,
	arTotalSupply: `https://arweave.net/total_supply`,
	arTxEndpoint: (txId) => `https://arweave.net/${txId}`,
	goldsky: `https://arweave-search.goldsky.com/graphql`,
	mainnetRpc: `https://ethereum.publicnode.com`,
};

const getTxEndpoint = (txId) => ENDPOINTS.arTxEndpoint(txId);

export const ASSETS = {
	arconnect: getTxEndpoint('1Q5zOfpHzHnNtD2BS6Rg50WWT2H8aq3GYThDV3x6Qo0'),
	arweaveApp: getTxEndpoint('CNZKujmn8vo0QM7Ssq18-3d0k6Azv1xsj20yAWt1Vew'),
	codehawksAudit: getTxEndpoint('rT-u8ijl3BLWDZlrQ3zpSNQ3gCz5GAq-LZRX4Gwo_yA'),
	morpheusAudit: getTxEndpoint('QbdAzvz1zVMpWw-9x-8W_0uBJt2lMVEryPKAVf0A9fw'),
	nccAudit: getTxEndpoint('BX5fd9Z4PijeWx7GFwgh-BjJRVNcnD1dPi4qo4vUDcA'),
	othent: getTxEndpoint('CV4m-XD_SYNKoxm7nh-3pxl_RdG9SdrOB2ibFmwCDPA'),
	renascenseAudit: getTxEndpoint('oVv6te32GQUC-qidsZZlUOp3nTdfPcGFN-UeW9brau4'),
	walletConnect: getTxEndpoint('llCUeYuxYxnH6rp2PVrkOR2pkGy0rFPR8wlIBbl-Ols'),
	add: getTxEndpoint('RLWnDhoB0Dd_X-sLnNy4w2S7ds3l9591HcHK8nc3YRw'),
	ao: getTxEndpoint('AzM59q2tcYzkySUUZUN1HCwfKGVHi--71UdoIk5gPUE'),
	aoCircled: getTxEndpoint('UkS-mdoiG8hcAClhKK8ch4ZhEzla0mCPDOix9hpdSFE'),
	arrow: getTxEndpoint('ghFL1fzQ2C1eEAnqSVvfAMP5Jikx7NKSPP5neoNPALw'),
	arweave: getTxEndpoint('LeeiCXkCDZKdh9uEfau2a13LziNGnT82anXFDW51Hgw'),
	checkmark: getTxEndpoint('mVnNwxm-F6CV043zVtORE-EaMWfd2j8w6HHX70IcVbI'),
	close: getTxEndpoint('BASlMnOWcLCcLUSrO2wUybQL_06231dLONeVkdTWs3o'),
	copy: getTxEndpoint('au_20PzacCJjUbwjoX85kkzmW0YwH4KrPfP98NOBK8M'),
	dai: getTxEndpoint('0fH_eBybJYRxjpjhJLiDoj8-7u7wYEHXtNElWEPb5is'),
	deposit: getTxEndpoint('KJtoIHxAHtVRMDgDGF00NYcnz81iUSo2o8odeDB623Q'),
	disconnect: getTxEndpoint('eWncZs2hH5oNSsWTIImJhqdZ4-n0P4CfZbduK2ae4L4'),
	discord: getTxEndpoint('3X1BfFleeCZZdVZIx8DKDIblcLw7jzzRBCzSItlBy9E'),
	edit: getTxEndpoint('SUWTk8Qtcub9EsP5PDF6-vzgKsP5Irg1bB9b8NImDDk'),
	ethereum: getTxEndpoint('LmRXPMcmymzB5S_WpRgmmQtGMWoTHW7BFcmotOkKcGM'),
	exchange: getTxEndpoint('KfE6Dh0j2pTLo4Z8U6fmk6mCRsB6O6NgxJpI_Vm0_wY'),
	github: getTxEndpoint('7JXQVvywkWNFXAyAPJ8WdC5VSk7d0q0E-c-6v-oM3iM'),
	info: getTxEndpoint('QQ4EJ_wH2EY1_ElfSNKffixnzVcbnvd2547lmluvT-0'),
	landingGraphic: getTxEndpoint('H6009sE8L1EOCjUOZzUVAH9gAI0ZMaQYPnEGcR63oJI'),
	link: getTxEndpoint('UMfjnj-8e7fb3lYRdcFygu8c4JoBZq3hB-mzycYT4DU'),
	menu: getTxEndpoint('0La3-o2_gGMDbkfV4zVVUMjTYQ7Cn9YWQ2JO-FbjAIk'),
	pi: getTxEndpoint('fGTu1CGT6TAz6Uj55CPkpJRy_whPKRZH6OFFpVHWOS0'),
	plus: getTxEndpoint('OUryhpUV-y709P_Tr575rN8gS-8c5rzlKXNymR9gsE4'),
	remove: getTxEndpoint('aKjWuVXkSeYOKzGP0MnnhHwoYUXqTHFMJfVCbqzYEo0'),
	stEth: getTxEndpoint('0SmAFjMZ5BmFPB_wlPeVJLhWGZ9JqAlV3sNozIPV2yk'),
	success: getTxEndpoint('mVnNwxm-F6CV043zVtORE-EaMWfd2j8w6HHX70IcVbI'),
	token: getTxEndpoint('f18VARM42GRSDY8UzZtEJrCsakbxluldOAnnED_V_Zk'),
	view: getTxEndpoint('LOxVL3vN3EkCqjbSxwuenYTTsbLtFJzK-lLJ6P4k59w'),
	wallet: getTxEndpoint('MMIDwWfe33ob3yD34eforpwPkhK-1BDVrTla6ZTX-3A'),
	wander: getTxEndpoint('0nDLgQik8oWPr0nSVEwI9B8D-XMEptQagNdsdr_y6Jk'),
	warning: getTxEndpoint('BASlMnOWcLCcLUSrO2wUybQL_06231dLONeVkdTWs3o'),
	website: getTxEndpoint('YBilSmUhX--T9vffUIDsCCrWoakxaxPqPVw7NCZNNVs'),
	withdraw: getTxEndpoint('QOJLKefBz2xCPUbO8dEKB22aWv_zdQ6FYA_UWUriyJw'),
	x: getTxEndpoint('8j0KOYorbeN1EI2_tO-o9tUYi4LJkDwFCDStu0sWMV8'),
	yield: getTxEndpoint('RrusyNB6RzmXfYcocp7tG9GSDkrF_z-_NfZMSxVgzOE'),
};

// export const AR_WALLETS = [
// 	{ type: ArWalletEnum.wander, logo: ASSETS.wander },
// 	{ type: ArWalletEnum.othent, logo: ASSETS.othent },
// 	{ type: ArWalletEnum.arweaveApp, logo: ASSETS.arweaveApp },
// ];

export const DOM = {
	loader: 'loader',
	notification: 'notification',
	overlay: 'overlay',
};

export const STYLING = {
	cutoffs: {
		desktop: '1200px',
		initial: '1024px',
		max: '1460px',
		tablet: '840px',
		tabletSecondary: '768px',
		secondary: '540px',
	},
	dimensions: {
		button: {
			height: '40px',
			width: 'fit-content',
		},
		nav: {
			height: '75px',
		},
		radius: {
			primary: '10px',
			alt1: '15px',
			alt2: '5px',
			alt3: '2.5px',
		},
	},
};

function createURLs() {
	const base = `/`;
	const mint = `${base}mint/`;
	return {
		base: base,
		mint: mint,
		mintDeposits: `${mint}deposits/`,
		mintYield: `${mint}yield/`,
		policies: `${base}policies/`,
		read: `${base}read/`,
		delegate: `${base}delegate/`,
		delegateDashboard: `${base}delegate/dashboard/`,
		notFound: `${base}404`,
	};
}

export const URLS = createURLs();

export const WALLET_PERMISSIONS = ['ACCESS_ADDRESS', 'ACCESS_PUBLIC_KEY', 'SIGN_TRANSACTION', 'DISPATCH', 'SIGNATURE'];

export const StEthBridge_ABI = [
	{ inputs: [], stateMutability: 'nonpayable', type: 'constructor' },
	{
		anonymous: false,
		inputs: [
			{ indexed: false, internalType: 'address', name: 'previousAdmin', type: 'address' },
			{ indexed: false, internalType: 'address', name: 'newAdmin', type: 'address' },
		],
		name: 'AdminChanged',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [{ indexed: true, internalType: 'address', name: 'beacon', type: 'address' }],
		name: 'BeaconUpgraded',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [{ indexed: false, internalType: 'uint8', name: 'version', type: 'uint8' }],
		name: 'Initialized',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [{ indexed: false, internalType: 'uint256', name: 'amount', type: 'uint256' }],
		name: 'OverplusBridged',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'address', name: 'previousOwner', type: 'address' },
			{ indexed: true, internalType: 'address', name: 'newOwner', type: 'address' },
		],
		name: 'OwnershipTransferred',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'uint256', name: 'poolId', type: 'uint256' },
			{
				components: [
					{ internalType: 'uint128', name: 'payoutStart', type: 'uint128' },
					{ internalType: 'uint128', name: 'decreaseInterval', type: 'uint128' },
					{ internalType: 'uint128', name: 'withdrawLockPeriod', type: 'uint128' },
					{ internalType: 'uint128', name: 'claimLockPeriod', type: 'uint128' },
					{ internalType: 'uint128', name: 'withdrawLockPeriodAfterStake', type: 'uint128' },
					{ internalType: 'uint256', name: 'initialReward', type: 'uint256' },
					{ internalType: 'uint256', name: 'rewardDecrease', type: 'uint256' },
					{ internalType: 'uint256', name: 'minimalStake', type: 'uint256' },
					{ internalType: 'bool', name: 'isPublic', type: 'bool' },
				],
				indexed: false,
				internalType: 'struct IDistribution.Pool',
				name: 'pool',
				type: 'tuple',
			},
		],
		name: 'PoolCreated',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'uint256', name: 'poolId', type: 'uint256' },
			{
				components: [
					{ internalType: 'uint128', name: 'payoutStart', type: 'uint128' },
					{ internalType: 'uint128', name: 'decreaseInterval', type: 'uint128' },
					{ internalType: 'uint128', name: 'withdrawLockPeriod', type: 'uint128' },
					{ internalType: 'uint128', name: 'claimLockPeriod', type: 'uint128' },
					{ internalType: 'uint128', name: 'withdrawLockPeriodAfterStake', type: 'uint128' },
					{ internalType: 'uint256', name: 'initialReward', type: 'uint256' },
					{ internalType: 'uint256', name: 'rewardDecrease', type: 'uint256' },
					{ internalType: 'uint256', name: 'minimalStake', type: 'uint256' },
					{ internalType: 'bool', name: 'isPublic', type: 'bool' },
				],
				indexed: false,
				internalType: 'struct IDistribution.Pool',
				name: 'pool',
				type: 'tuple',
			},
		],
		name: 'PoolEdited',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [{ indexed: true, internalType: 'address', name: 'implementation', type: 'address' }],
		name: 'Upgraded',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'uint256', name: 'poolId', type: 'uint256' },
			{ indexed: true, internalType: 'address', name: 'user', type: 'address' },
			{ indexed: false, internalType: 'address', name: 'receiver', type: 'address' },
			{ indexed: false, internalType: 'uint256', name: 'amount', type: 'uint256' },
		],
		name: 'UserClaimed',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'uint256', name: 'poolId', type: 'uint256' },
			{ indexed: true, internalType: 'address', name: 'user', type: 'address' },
			{ indexed: false, internalType: 'uint256', name: 'amount', type: 'uint256' },
			{ indexed: false, internalType: 'bytes32', name: 'arweaveAddress', type: 'bytes32' },
		],
		name: 'UserStaked',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'uint256', name: 'poolId', type: 'uint256' },
			{ indexed: true, internalType: 'address', name: 'user', type: 'address' },
			{ indexed: false, internalType: 'uint256', name: 'amount', type: 'uint256' },
			{ indexed: false, internalType: 'bytes32', name: 'arweaveAddress', type: 'bytes32' },
		],
		name: 'UserWithdrawn',
		type: 'event',
	},
	{
		inputs: [
			{ internalType: 'address', name: 'depositToken_', type: 'address' },
			{ internalType: 'address', name: 'aoDistributionWallet_', type: 'address' },
			{
				components: [
					{ internalType: 'uint128', name: 'payoutStart', type: 'uint128' },
					{ internalType: 'uint128', name: 'decreaseInterval', type: 'uint128' },
					{ internalType: 'uint128', name: 'withdrawLockPeriod', type: 'uint128' },
					{ internalType: 'uint128', name: 'claimLockPeriod', type: 'uint128' },
					{ internalType: 'uint128', name: 'withdrawLockPeriodAfterStake', type: 'uint128' },
					{ internalType: 'uint256', name: 'initialReward', type: 'uint256' },
					{ internalType: 'uint256', name: 'rewardDecrease', type: 'uint256' },
					{ internalType: 'uint256', name: 'minimalStake', type: 'uint256' },
					{ internalType: 'bool', name: 'isPublic', type: 'bool' },
				],
				internalType: 'struct IDistribution.Pool[]',
				name: 'poolsInfo_',
				type: 'tuple[]',
			},
			{ internalType: 'address', name: 'refunderAddress_', type: 'address' },
			{ internalType: 'address', name: 'fallbackAddress_', type: 'address' },
		],
		name: 'Distribution_init',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'aoDistributionWallet',
		outputs: [{ internalType: 'address', name: '', type: 'address' }],
		stateMutability: 'view',
		type: 'function',
	},
	{ inputs: [], name: 'bridgeOverplus', outputs: [], stateMutability: 'nonpayable', type: 'function' },
	{
		inputs: [
			{
				components: [
					{ internalType: 'uint128', name: 'payoutStart', type: 'uint128' },
					{ internalType: 'uint128', name: 'decreaseInterval', type: 'uint128' },
					{ internalType: 'uint128', name: 'withdrawLockPeriod', type: 'uint128' },
					{ internalType: 'uint128', name: 'claimLockPeriod', type: 'uint128' },
					{ internalType: 'uint128', name: 'withdrawLockPeriodAfterStake', type: 'uint128' },
					{ internalType: 'uint256', name: 'initialReward', type: 'uint256' },
					{ internalType: 'uint256', name: 'rewardDecrease', type: 'uint256' },
					{ internalType: 'uint256', name: 'minimalStake', type: 'uint256' },
					{ internalType: 'bool', name: 'isPublic', type: 'bool' },
				],
				internalType: 'struct IDistribution.Pool',
				name: 'pool_',
				type: 'tuple',
			},
		],
		name: 'createPool',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'depositToken',
		outputs: [{ internalType: 'address', name: '', type: 'address' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'uint256', name: 'poolId', type: 'uint256' },
			{ internalType: 'address', name: 'user', type: 'address' },
		],
		name: 'ejectStakedFunds',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'uint256', name: 'poolId_', type: 'uint256' },
			{ internalType: 'address', name: 'user_', type: 'address' },
		],
		name: 'getCurrentUserReward',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'uint256', name: 'poolId_', type: 'uint256' },
			{ internalType: 'uint128', name: 'startTime_', type: 'uint128' },
			{ internalType: 'uint128', name: 'endTime_', type: 'uint128' },
		],
		name: 'getPeriodReward',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'isNotUpgradeable',
		outputs: [{ internalType: 'bool', name: '', type: 'bool' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'overplus',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'owner',
		outputs: [{ internalType: 'address', name: '', type: 'address' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		name: 'pools',
		outputs: [
			{ internalType: 'uint128', name: 'payoutStart', type: 'uint128' },
			{ internalType: 'uint128', name: 'decreaseInterval', type: 'uint128' },
			{ internalType: 'uint128', name: 'withdrawLockPeriod', type: 'uint128' },
			{ internalType: 'uint128', name: 'claimLockPeriod', type: 'uint128' },
			{ internalType: 'uint128', name: 'withdrawLockPeriodAfterStake', type: 'uint128' },
			{ internalType: 'uint256', name: 'initialReward', type: 'uint256' },
			{ internalType: 'uint256', name: 'rewardDecrease', type: 'uint256' },
			{ internalType: 'uint256', name: 'minimalStake', type: 'uint256' },
			{ internalType: 'bool', name: 'isPublic', type: 'bool' },
		],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		name: 'poolsData',
		outputs: [
			{ internalType: 'uint128', name: 'lastUpdate', type: 'uint128' },
			{ internalType: 'uint256', name: 'rate', type: 'uint256' },
			{ internalType: 'uint256', name: 'totalDeposited', type: 'uint256' },
		],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'proxiableUUID',
		outputs: [{ internalType: 'bytes32', name: '', type: 'bytes32' }],
		stateMutability: 'view',
		type: 'function',
	},
	{ inputs: [], name: 'removeUpgradeability', outputs: [], stateMutability: 'nonpayable', type: 'function' },
	{ inputs: [], name: 'renounceOwnership', outputs: [], stateMutability: 'nonpayable', type: 'function' },
	{
		inputs: [
			{ internalType: 'uint256', name: 'poolId_', type: 'uint256' },
			{ internalType: 'uint256', name: 'amount_', type: 'uint256' },
			{ internalType: 'bytes32', name: 'arweaveAddress_', type: 'bytes32' },
		],
		name: 'stake',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'totalDepositedInPublicPools',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'address', name: 'newOwner', type: 'address' }],
		name: 'transferOwnership',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'address', name: 'newImplementation', type: 'address' }],
		name: 'upgradeTo',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: 'newImplementation', type: 'address' },
			{ internalType: 'bytes', name: 'data', type: 'bytes' },
		],
		name: 'upgradeToAndCall',
		outputs: [],
		stateMutability: 'payable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: '', type: 'address' },
			{ internalType: 'uint256', name: '', type: 'uint256' },
		],
		name: 'usersData',
		outputs: [
			{ internalType: 'uint128', name: 'lastStake', type: 'uint128' },
			{ internalType: 'uint256', name: 'deposited', type: 'uint256' },
			{ internalType: 'uint256', name: 'rate', type: 'uint256' },
			{ internalType: 'uint256', name: 'pendingRewards', type: 'uint256' },
		],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'uint256', name: 'poolId_', type: 'uint256' },
			{ internalType: 'uint256', name: 'amount_', type: 'uint256' },
			{ internalType: 'bytes32', name: 'arweaveAddress_', type: 'bytes32' },
		],
		name: 'withdraw',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
];

export const DaiBridge_ABI = [
	{ inputs: [], stateMutability: 'nonpayable', type: 'constructor' },
	{
		anonymous: false,
		inputs: [
			{
				indexed: false,
				internalType: 'address',
				name: 'previousAdmin',
				type: 'address',
			},
			{
				indexed: false,
				internalType: 'address',
				name: 'newAdmin',
				type: 'address',
			},
		],
		name: 'AdminChanged',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: true,
				internalType: 'address',
				name: 'beacon',
				type: 'address',
			},
		],
		name: 'BeaconUpgraded',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: false,
				internalType: 'uint256',
				name: 'dsrDaiBalance',
				type: 'uint256',
			},
			{
				indexed: false,
				internalType: 'uint256',
				name: 'currentOverplus',
				type: 'uint256',
			},
		],
		name: 'CalculateOverplusResult',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: false,
				internalType: 'uint8',
				name: 'version',
				type: 'uint8',
			},
		],
		name: 'Initialized',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: false,
				internalType: 'uint256',
				name: 'amount',
				type: 'uint256',
			},
		],
		name: 'OverplusBridged',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: true,
				internalType: 'address',
				name: 'previousOwner',
				type: 'address',
			},
			{
				indexed: true,
				internalType: 'address',
				name: 'newOwner',
				type: 'address',
			},
		],
		name: 'OwnershipTransferred',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: true,
				internalType: 'uint256',
				name: 'poolId',
				type: 'uint256',
			},
			{
				components: [
					{
						internalType: 'uint128',
						name: 'withdrawLockPeriodAfterStake',
						type: 'uint128',
					},
					{
						internalType: 'uint256',
						name: 'minimalStake',
						type: 'uint256',
					},
					{ internalType: 'bool', name: 'isPublic', type: 'bool' },
				],
				indexed: false,
				internalType: 'struct IDistribution.Pool',
				name: 'pool',
				type: 'tuple',
			},
		],
		name: 'PoolCreated',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: true,
				internalType: 'uint256',
				name: 'poolId',
				type: 'uint256',
			},
			{
				components: [
					{
						internalType: 'uint128',
						name: 'withdrawLockPeriodAfterStake',
						type: 'uint128',
					},
					{
						internalType: 'uint256',
						name: 'minimalStake',
						type: 'uint256',
					},
					{ internalType: 'bool', name: 'isPublic', type: 'bool' },
				],
				indexed: false,
				internalType: 'struct IDistribution.Pool',
				name: 'pool',
				type: 'tuple',
			},
		],
		name: 'PoolEdited',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: true,
				internalType: 'address',
				name: 'implementation',
				type: 'address',
			},
		],
		name: 'Upgraded',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: true,
				internalType: 'uint256',
				name: 'poolId',
				type: 'uint256',
			},
			{
				indexed: true,
				internalType: 'address',
				name: 'user',
				type: 'address',
			},
			{
				indexed: false,
				internalType: 'address',
				name: 'receiver',
				type: 'address',
			},
			{
				indexed: false,
				internalType: 'uint256',
				name: 'amount',
				type: 'uint256',
			},
		],
		name: 'UserClaimed',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: true,
				internalType: 'uint256',
				name: 'poolId',
				type: 'uint256',
			},
			{
				indexed: true,
				internalType: 'address',
				name: 'user',
				type: 'address',
			},
			{
				indexed: false,
				internalType: 'uint256',
				name: 'amount',
				type: 'uint256',
			},
			{
				indexed: false,
				internalType: 'bytes32',
				name: 'arweaveAddress',
				type: 'bytes32',
			},
		],
		name: 'UserStaked',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{
				indexed: true,
				internalType: 'uint256',
				name: 'poolId',
				type: 'uint256',
			},
			{
				indexed: true,
				internalType: 'address',
				name: 'user',
				type: 'address',
			},
			{
				indexed: false,
				internalType: 'uint256',
				name: 'amount',
				type: 'uint256',
			},
			{
				indexed: false,
				internalType: 'bytes32',
				name: 'arweaveAddress',
				type: 'bytes32',
			},
		],
		name: 'UserWithdrawn',
		type: 'event',
	},
	{
		inputs: [
			{
				internalType: 'address',
				name: 'aoDistributionWallet_',
				type: 'address',
			},
			{
				components: [
					{
						internalType: 'uint128',
						name: 'withdrawLockPeriodAfterStake',
						type: 'uint128',
					},
					{
						internalType: 'uint256',
						name: 'minimalStake',
						type: 'uint256',
					},
					{ internalType: 'bool', name: 'isPublic', type: 'bool' },
				],
				internalType: 'struct IDistribution.Pool[]',
				name: 'poolsInfo_',
				type: 'tuple[]',
			},
			{
				internalType: 'address',
				name: 'refunderAddress_',
				type: 'address',
			},
			{
				internalType: 'address',
				name: 'fallbackAddress_',
				type: 'address',
			},
		],
		name: 'Distribution_init',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'aoDistributionWallet',
		outputs: [{ internalType: 'address', name: '', type: 'address' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'bridgeOverplus',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'calculateOverplus',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{
				components: [
					{
						internalType: 'uint128',
						name: 'withdrawLockPeriodAfterStake',
						type: 'uint128',
					},
					{
						internalType: 'uint256',
						name: 'minimalStake',
						type: 'uint256',
					},
					{ internalType: 'bool', name: 'isPublic', type: 'bool' },
				],
				internalType: 'struct IDistribution.Pool',
				name: 'pool_',
				type: 'tuple',
			},
		],
		name: 'createPool',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'daiDsrManager',
		outputs: [
			{
				internalType: 'contract IDaiDsrManager',
				name: '',
				type: 'address',
			},
		],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'daiDsrManagerAddress',
		outputs: [{ internalType: 'address', name: '', type: 'address' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'depositToken',
		outputs: [{ internalType: 'address', name: '', type: 'address' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'uint256', name: 'poolId', type: 'uint256' },
			{ internalType: 'address', name: 'user', type: 'address' },
		],
		name: 'ejectStakedFunds',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'isNotUpgradeable',
		outputs: [{ internalType: 'bool', name: '', type: 'bool' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'owner',
		outputs: [{ internalType: 'address', name: '', type: 'address' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		name: 'pools',
		outputs: [
			{ internalType: 'uint128', name: 'withdrawLockPeriodAfterStake', type: 'uint128' },
			{ internalType: 'uint256', name: 'minimalStake', type: 'uint256' },
			{ internalType: 'bool', name: 'isPublic', type: 'bool' },
		],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		name: 'poolsData',
		outputs: [
			{ internalType: 'uint128', name: 'lastUpdate', type: 'uint128' },
			{ internalType: 'uint256', name: 'totalDeposited', type: 'uint256' },
		],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'proxiableUUID',
		outputs: [{ internalType: 'bytes32', name: '', type: 'bytes32' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'removeUpgradeability',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'renounceOwnership',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'uint256', name: 'poolId_', type: 'uint256' },
			{ internalType: 'uint256', name: 'amount_', type: 'uint256' },
			{
				internalType: 'bytes32',
				name: 'arweaveAddress_',
				type: 'bytes32',
			},
		],
		name: 'stake',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'totalDepositedInPublicPools',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'address', name: 'newOwner', type: 'address' }],
		name: 'transferOwnership',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{
				internalType: 'address',
				name: 'newImplementation',
				type: 'address',
			},
		],
		name: 'upgradeTo',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{
				internalType: 'address',
				name: 'newImplementation',
				type: 'address',
			},
			{ internalType: 'bytes', name: 'data', type: 'bytes' },
		],
		name: 'upgradeToAndCall',
		outputs: [],
		stateMutability: 'payable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: '', type: 'address' },
			{ internalType: 'uint256', name: '', type: 'uint256' },
		],
		name: 'usersData',
		outputs: [
			{ internalType: 'uint128', name: 'lastStake', type: 'uint128' },
			{ internalType: 'uint256', name: 'deposited', type: 'uint256' },
		],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'uint256', name: 'poolId_', type: 'uint256' },
			{ internalType: 'uint256', name: 'amount_', type: 'uint256' },
			{
				internalType: 'bytes32',
				name: 'arweaveAddress_',
				type: 'bytes32',
			},
		],
		name: 'withdraw',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
];

export const Erc20_ABI = [
	{ inputs: [], stateMutability: 'nonpayable', type: 'constructor' },
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'address', name: 'owner', type: 'address' },
			{ indexed: true, internalType: 'address', name: 'spender', type: 'address' },
			{ indexed: false, internalType: 'uint256', name: 'value', type: 'uint256' },
		],
		name: 'Approval',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'address', name: 'previousOwner', type: 'address' },
			{ indexed: true, internalType: 'address', name: 'newOwner', type: 'address' },
		],
		name: 'OwnershipTransferred',
		type: 'event',
	},
	{
		anonymous: false,
		inputs: [
			{ indexed: true, internalType: 'address', name: 'from', type: 'address' },
			{ indexed: true, internalType: 'address', name: 'to', type: 'address' },
			{ indexed: false, internalType: 'uint256', name: 'value', type: 'uint256' },
		],
		name: 'Transfer',
		type: 'event',
	},
	{
		inputs: [
			{ internalType: 'address', name: 'owner', type: 'address' },
			{ internalType: 'address', name: 'spender', type: 'address' },
		],
		name: 'allowance',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: 'spender', type: 'address' },
			{ internalType: 'uint256', name: 'amount', type: 'uint256' },
		],
		name: 'approve',
		outputs: [{ internalType: 'bool', name: '', type: 'bool' }],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'address', name: '_account', type: 'address' }],
		name: 'balanceOf',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'decimals',
		outputs: [{ internalType: 'uint8', name: '', type: 'uint8' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: 'spender', type: 'address' },
			{ internalType: 'uint256', name: 'subtractedValue', type: 'uint256' },
		],
		name: 'decreaseAllowance',
		outputs: [{ internalType: 'bool', name: '', type: 'bool' }],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'uint256', name: '_sharesAmount', type: 'uint256' }],
		name: 'getPooledEthByShares',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'uint256', name: '_ethAmount', type: 'uint256' }],
		name: 'getSharesByPooledEth',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: 'spender', type: 'address' },
			{ internalType: 'uint256', name: 'addedValue', type: 'uint256' },
		],
		name: 'increaseAllowance',
		outputs: [{ internalType: 'bool', name: '', type: 'bool' }],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: '_account', type: 'address' },
			{ internalType: 'uint256', name: '_amount', type: 'uint256' },
		],
		name: 'mint',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [],
		name: 'name',
		outputs: [{ internalType: 'string', name: '', type: 'string' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'owner',
		outputs: [{ internalType: 'address', name: '', type: 'address' }],
		stateMutability: 'view',
		type: 'function',
	},
	{ inputs: [], name: 'renounceOwnership', outputs: [], stateMutability: 'nonpayable', type: 'function' },
	{
		inputs: [{ internalType: 'uint256', name: '_totalPooledEther', type: 'uint256' }],
		name: 'setTotalPooledEther',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'address', name: '_account', type: 'address' }],
		name: 'sharesOf',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'symbol',
		outputs: [{ internalType: 'string', name: '', type: 'string' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'totalPooledEther',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'totalShares',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [],
		name: 'totalSupply',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'view',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: 'to', type: 'address' },
			{ internalType: 'uint256', name: 'amount', type: 'uint256' },
		],
		name: 'transfer',
		outputs: [{ internalType: 'bool', name: '', type: 'bool' }],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: 'from', type: 'address' },
			{ internalType: 'address', name: 'to', type: 'address' },
			{ internalType: 'uint256', name: 'amount', type: 'uint256' },
		],
		name: 'transferFrom',
		outputs: [{ internalType: 'bool', name: '', type: 'bool' }],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [{ internalType: 'address', name: 'newOwner', type: 'address' }],
		name: 'transferOwnership',
		outputs: [],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: '_recipient', type: 'address' },
			{ internalType: 'uint256', name: '_sharesAmount', type: 'uint256' },
		],
		name: 'transferShares',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'nonpayable',
		type: 'function',
	},
	{
		inputs: [
			{ internalType: 'address', name: '_sender', type: 'address' },
			{ internalType: 'address', name: '_recipient', type: 'address' },
			{ internalType: 'uint256', name: '_sharesAmount', type: 'uint256' },
		],
		name: 'transferSharesFrom',
		outputs: [{ internalType: 'uint256', name: '', type: 'uint256' }],
		stateMutability: 'nonpayable',
		type: 'function',
	},
];

export const PRICE_FEED_ABI = [
	{
		inputs: [],
		name: 'latestRoundData',
		outputs: [
			{ internalType: 'uint80', name: 'roundId', type: 'uint80' },
			{ internalType: 'int256', name: 'answer', type: 'int256' },
			{ internalType: 'uint256', name: 'startedAt', type: 'uint256' },
			{ internalType: 'uint256', name: 'updatedAt', type: 'uint256' },
			{ internalType: 'uint80', name: 'answeredInRound', type: 'uint80' },
		],
		stateMutability: 'view',
		type: 'function',
	},
];

export const REDIRECTS = {
	arconnect: `https://arconnect.io`,
	cookbook: `https://cookbook_ao.arweave.net`,
	x: `http://x.com/aoTheComputer`,
	github: `https://github.com/permaweb/ao`,
	discord: `https://discord.gg/dYXtHwc9dc`,
	stethMinting: `https://stake.lido.fi/`,
	wander: `https://www.wander.app/`,
	stethConversion: `https://matcha.xyz/tokens/ethereum/eth?buyChain=1&buyAddress=0xae7ab96520de3a18e5e111b5eaab095312d7fe84`,
	tokenomics: `https://mirror.xyz/0x1EE4bE8670E8Bd7E9E2E366F530467030BE4C840/-UWra0q0KWecSpgg2-c37dbZ0lnOMEScEEkabVm9qaQ`,
	ipBlock: `https://www.standwithcrypto.org/action/call?action=call-your-representative`,
	viewblock: (address) => `https://viewblock.io/arweave/address/${address}`,
	etherscan: (address) => `https://etherscan.io/address/${address}`,
};

export const ETH_EXCHANGE_CONFIG = {
	arweave: {
		description: `Owners of AR generate AO continuously, proportionate to their holdings. You do not need to perform any form of activation in order to receive these tokens.
This page will help you keep track of your AO rewards and future projections. Simply connect your Arweave wallet to view your balance.
AO tokens will become transferrable after 15% of the supply has been minted, on approximately February 8th, 2025. Learn more in the <a href="https://mirror.xyz/0x1EE4bE8670E8Bd7E9E2E366F530467030BE4C840/-UWra0q0KWecSpgg2-c37dbZ0lnOMEScEEkabVm9qaQ" target="_blank">blog post</a>.`,
	},
	dai: {
		description: `66.6% of AO tokens are minted to users that bridge their assets to the network. Simply connect your wallet, deposit Dai, and earn AO.
You will begin to accrue AO 24 hours after your deposit has been confirmed.<br/><br/>DAI has an 18-hour minimum lockup period. This means that you will not be able to remove your DAI from the bridge for 18 hours after depositing it.
AO tokens will become transferrable after 15% of the supply has been minted, on approximately February 8th, 2025. Learn more in the <a href="https://mirror.xyz/0x1EE4bE8670E8Bd7E9E2E366F530467030BE4C840/-UWra0q0KWecSpgg2-c37dbZ0lnOMEScEEkabVm9qaQ" target="_blank">blog post</a>.`,
	},
	stEth: {
		description: `66.6% of AO tokens are minted to users that bridge their assets to the network. Simply connect your wallet, deposit staked Ethereum, and earn AO.
You can remove your deposited tokens at any time. You will begin to accrue AO 24 hours after your deposit has been confirmed.<br/><br/>AO tokens will become transferrable after 15% of the supply has been minted, on approximately February 8th, 2025. Learn more in the <a href="https://mirror.xyz/0x1EE4bE8670E8Bd7E9E2E366F530467030BE4C840/-UWra0q0KWecSpgg2-c37dbZ0lnOMEScEEkabVm9qaQ" target="_blank">blog post</a>.`,
	},
	cred: {
		description: `Users that took part in AO testnet quests are able to convert their CRED tokens for AO-CLAIMs, at a rate of 1:1000.
AO tokens have a 100% fair launch, with zero pre-allocations of any kind. As a consequence, the AO provided to those that convert their CRED will be purchased or earned via holding AR by ecosystem parties that have volunteered to do so.
AO-claims will become redeemable after 15% of the AO supply has been minted, on approximately February 8th, 2025. Learn more in the <a href="https://mirror.xyz/0x1EE4bE8670E8Bd7E9E2E366F530467030BE4C840/ydfvlhml1NI9DdTps3nEX634AY5JaQD4WmFGtRBryzk" target="_blank">blog post</a>.
`,
	},
};

export const ETH_EXCHANGE_REDIRECTS = {
	ncc1: 'https://arweave.net/jZHVGxxxVpjGxD_uwpp-NSsezf9_z0r0evhDnV2hFNs',
	ncc2: 'https://arweave.net/qWdHQIGjeAjc5U5O9gk_o2k4jRYO6khL1vOAGQzkd9Y',
	morpheus:
		'https://github.com/MorpheusAIs/Docs/blob/main/Security%20Audit%20Reports/Distribution%20Contract/Distribution%20V1%20Audit%20%7C%20Community.md',
	codehawks:
		'https://github.com/MorpheusAIs/Docs/blob/main/Security%20Audit%20Reports/Distribution%20Contract/Distribution%20V1%20Public%20Bug%20Bounty%20%7C%20Code%20Hawks.md',
	renascence:
		'https://github.com/MorpheusAIs/Docs/blob/main/Security%20Audit%20Reports/Distribution%20Contract/Distribution%20V2%20Audit%20%7C%20Renascence.pdf',
};

export const NAV_REDIRECTS = [
	{ path: REDIRECTS.discord, label: 'Discord' },
	{ path: REDIRECTS.x, label: 'X' },
	{ path: REDIRECTS.github, label: 'GitHub' },
	{ path: URLS.policies, label: 'Policies' },
];