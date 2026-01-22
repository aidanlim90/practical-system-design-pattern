import * as cdk from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as ecsPatterns from 'aws-cdk-lib/aws-ecs-patterns';
import * as apigwv2 from 'aws-cdk-lib/aws-apigatewayv2';
import * as apigwv2Integrations from 'aws-cdk-lib/aws-apigatewayv2-integrations';

export class ApiGatewayEcsStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    // VPC with public + private subnets (private for Fargate, with egress for pulling images)
    const vpc = new ec2.Vpc(this, 'Vpc', {
      maxAzs: 2,
      natGateways: 1,
    });

    // Security Group for VPC Link (allows API Gateway to reach ALB)
    const vpcLinkSg = new ec2.SecurityGroup(this, 'VpcLinkSg', {
      vpc,
      description: 'Security group for API Gateway VPC Link',
      allowAllOutbound: true,
    });
    vpcLinkSg.addIngressRule(vpcLinkSg, ec2.Port.tcp(80), 'Self-reference');

    // ECS Cluster
    const cluster = new ecs.Cluster(this, 'Cluster', { vpc });

    // Fargate Service with official .NET sample app
    const fargateService = new ecsPatterns.ApplicationLoadBalancedFargateService(this, 'DotNetService', {
      cluster,
      taskImageOptions: {
        image: ecs.ContainerImage.fromRegistry('mendhak/http-https-echo:latest'),
        containerPort: 8080,  // Critical: .NET 8+ official images listen on 8080
      },
      publicLoadBalancer: false,  // Internal ALB only
      desiredCount: 1,
    });

    // Allow traffic from VPC Link SG to ALB listener
    fargateService.loadBalancer.connections.allowFrom(
      vpcLinkSg,
      ec2.Port.tcp(80),
      'Allow API Gateway VPC Link to ALB'
    );

    // Health check (the sample app responds on root)
    fargateService.targetGroup.configureHealthCheck({ path: '/' });

    // VPC Link (HTTP API style - no targets needed, just VPC + SG)
    const vpcLink = new apigwv2.VpcLink(this, 'VpcLink', {
      vpc,
      securityGroups: [vpcLinkSg],
      subnets: vpc.selectSubnets({ subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS }),
    });

    // HTTP API
    const httpApi = new apigwv2.HttpApi(this, 'HttpApi', {
      apiName: 'OrderServiceApi',
    });

    // ALB Integration via VPC Link
    const albIntegration = new apigwv2Integrations.HttpAlbIntegration('AlbIntegration', fargateService.listener, {
      vpcLink,
    });

    // Routes: exact /order-service + proxy for all subpaths
    httpApi.addRoutes({
      path: '/order-service',
      methods: [apigwv2.HttpMethod.ANY],
      integration: albIntegration,
    });

    httpApi.addRoutes({
      path: '/order-service/{proxy+}',
      methods: [apigwv2.HttpMethod.ANY],
      integration: albIntegration,
    });

    // Helpful outputs
    new cdk.CfnOutput(this, 'ApiEndpoint', {
      value: httpApi.apiEndpoint,
      description: 'Base API Gateway URL',
    });

    new cdk.CfnOutput(this, 'OrderServiceUrl', {
      value: `${httpApi.apiEndpoint}/order-service/`,
      description: 'Full URL to your .NET service (add trailing slash)',
    });
  }
}