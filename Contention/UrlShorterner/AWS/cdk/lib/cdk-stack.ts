import * as cdk from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as route53 from 'aws-cdk-lib/aws-route53';
import * as route53Targets from 'aws-cdk-lib/aws-route53-targets';
import * as certmgr from 'aws-cdk-lib/aws-certificatemanager';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as elbv2 from 'aws-cdk-lib/aws-elasticloadbalancingv2';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import * as elasticache from 'aws-cdk-lib/aws-elasticache';

export class UrlShortenerStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const vpc = new ec2.Vpc(this, 'UrlShortenerVpc1', {
      ipAddresses: ec2.IpAddresses.cidr('10.1.0.0/16'),
      maxAzs: 2,
      natGateways: 1,
    });

    // const hostedZone = new route53.HostedZone(this, 'UrlShortenerHostedZone', {
    //   zoneName: 'gluebify.com',
    // });

    // const certificate = new certmgr.Certificate(this, 'UrlShortenerCertificate', {
    //   domainName: '*.gluebify.com',
    //   subjectAlternativeNames: ['gluebify.com'],
    //   validation: certmgr.CertificateValidation.fromDns(hostedZone),
    // });

    // const authTokenSecret = new secretsmanager.Secret(this, 'RedisAuthToken', {
    //   description: 'Auth token for URL shortener Redis cluster',
    //   generateSecretString: {
    //     secretStringTemplate: JSON.stringify({ username: 'default' }),
    //     generateStringKey: 'password',
    //     excludeCharacters: '"@/\\\'',
    //     passwordLength: 32,
    //     requireEachIncludedType: true
    //   }
    // });

    // const subnetGroup = new elasticache.CfnSubnetGroup(this, 'UrlShortenerCacheSubnetGroup', {
    //   description: 'Subnet group for URL shortener cache',
    //   subnetIds: vpc.privateSubnets.map(subnet => subnet.subnetId),
    // });

    // const redisSecurityGroup = new ec2.SecurityGroup(this, 'UrlShortenerCacheSecurityGroup', {
    //   vpc: vpc,
    //   description: 'Security group for URL shortener ElastiCache cluster',
    // });

    // const replicationGroup = new elasticache.CfnReplicationGroup(this, 'UrlShortenerCache', {
    //   replicationGroupId: 'url-shortener-cache',
    //   replicationGroupDescription: 'Redis cluster for URL shortener application',
    //   engine: 'redis',
    //   cacheNodeType: 'cache.t2.micro',
    //   numNodeGroups: 1, // For cluster mode disabled
    //   replicasPerNodeGroup: 1, // Creates a primary and one replica (total 2 nodes)
    //   automaticFailoverEnabled: true,
    //   multiAzEnabled: true,
    //   cacheSubnetGroupName: subnetGroup.ref,
    //   securityGroupIds: [redisSecurityGroup.securityGroupId],
    //   transitEncryptionEnabled: true,
    //   atRestEncryptionEnabled: true,
    //   authToken: authTokenSecret.secretValueFromJson('password').unsafeUnwrap(),
    //   autoMinorVersionUpgrade: true,
    //   // Add a dependency to ensure subnet group is created first
    // });
    // replicationGroup.addDependency(subnetGroup);

    // const cluster = new ecs.Cluster(this, 'UrlShortenerEcsCluster', {
    //   vpc: vpc,
    // });

    // const taskDefinition = new ecs.FargateTaskDefinition(this, 'UrlShortenerTaskDefinition', {
    //   memoryLimitMiB: 512,
    //   cpu: 256,
    // });
    
    // authTokenSecret.grantRead(taskDefinition.taskRole);

    // taskDefinition.addContainer('UrlShortenerContainer', {
    //   image: ecs.ContainerImage.fromRegistry('mcr.microsoft.com/dotnet/samples:aspnetapp'),
    //   containerName: 'aspnetcore_sample',
    //   portMappings: [{ containerPort: 8080 }],
    //   // Inject environment variables
    //   environment: {
    //     // Pass the Redis endpoint and port as plain text environment variables
    //     "ASPNETCORE_ENVIRONMENT": "Production",
    //     "Redis__Host": replicationGroup.attrPrimaryEndPointAddress,
    //     "Redis__Port": replicationGroup.attrPrimaryEndPointPort,
    //   },
    //   secrets: {
    //     // Securely inject the Redis password from Secrets Manager
    //     // The key 'Redis__Password' will be the environment variable name in the container
    //     "Redis__Password": ecs.Secret.fromSecretsManager(authTokenSecret, 'password'),
    //   },
    //   logging: ecs.LogDrivers.awsLogs({ streamPrefix: 'UrlShortener' }),
    // });

    // const service = new ecs.FargateService(this, 'UrlShortenerFargateService', {
    //   cluster: cluster,
    //   taskDefinition: taskDefinition,
    //   desiredCount: 1,
    //   assignPublicIp: false, // Best practice to keep services in private subnets
    // });

    // redisSecurityGroup.addIngressRule(
    //   service.connections.securityGroups[0],
    //   ec2.Port.tcp(6379),
    //   'Allow Redis access from ECS Service'
    // );

    // const lb = new elbv2.ApplicationLoadBalancer(this, 'UrlShortenerLoadBalancer', {
    //   vpc: vpc,
    //   internetFacing: true,
    // });

    // const listener = lb.addListener('UrlShortenerHttpsListener', {
    //   port: 443,
    //   certificates: [certificate],
    // });

    // listener.addTargets('UrlShortenerEcsTarget', {
    //   port: 8080,
    //   targets: [service],
    //   protocol: elbv2.ApplicationProtocol.HTTP,
    //   healthCheck: {
    //     path: '/', // A dedicated health check endpoint is recommended
    //     interval: cdk.Duration.seconds(30),
    //   },
    // });

    // lb.addRedirect({
    //   sourceProtocol: elbv2.ApplicationProtocol.HTTP,
    //   sourcePort: 80,
    //   targetProtocol: elbv2.ApplicationProtocol.HTTPS,
    //   targetPort: 443,
    // });

    // new route53.ARecord(this, 'UrlShortenerAliasRecord', {
    //   zone: hostedZone,
    //   recordName: 'api.gluebify.com',
    //   target: route53.RecordTarget.fromAlias(new route53Targets.LoadBalancerTarget(lb)),
    // });

    // new cdk.CfnOutput(this, 'RedisPrimaryEndpoint', {
    //   value: `${replicationGroup.attrPrimaryEndPointAddress}:${replicationGroup.attrPrimaryEndPointPort}`,
    //   description: 'ElastiCache cluster primary endpoint'
    // });

    // new cdk.CfnOutput(this, 'RedisReadEndpoint', {
    //   value: `${replicationGroup.attrReadEndPointAddresses}:${replicationGroup.attrReadEndPointPorts}`,
    //   description: 'ElastiCache cluster read endpoint'
    // });
    
    // new cdk.CfnOutput(this, 'AuthTokenSecretArn', {
    //   value: authTokenSecret.secretArn,
    //   description: 'ARN of the secret containing the Redis auth token'
    // });
  }
}